from functools import wraps
from flask import request, redirect, session, current_app
import operator
from globals import db


# Decorator to help views verify whether the user is on the right page
def verify_correct_page(f):
    """
    A decorator to be used on routes/views, which checks the the user is on the correct page. Checks
    ``session['currentUrl']``. If the user is on the wrong page, they will be redirected to the correct page.

    .. note::
        * Should be used just on routes after the session is created (for example, after the initial questionnaire).
        * The ``/submit`` path is where the value of ``session['currentUrl']`` is set.
    """
    @wraps(f)
    def decorated_function(*args, **kwargs):
        currentUrl = request.url.replace(request.url_root, "")

        # Don't allow users to skip things or go back. Redirect to the correct page if they try.
        if 'currentUrl' in session and currentUrl != session['currentUrl']:
            return redirect(current_app.config["APPLICATION_ROOT"] + "/" + session['currentUrl'])

        # If user hasn't been here before, set their current URL to the first one in the list.
        if not 'currentUrl' in session:
            session['currentUrl'] = flat_page_list()[0]['path']

            # If the user happens to be on the page they are already supposed to be at, continue as normal
            if session['currentUrl'] == currentUrl:
                return f(*args, **kwargs)

            return redirect(current_app.config["APPLICATION_ROOT"] + "/" + session['currentUrl'])

        return f(*args, **kwargs)
    return decorated_function


def verify_session_valid(f):
    """
    A decorator to be used on routes/views, which checks for the existence of the 'currentUrl' key in ``session``.

    .. note::
        * Should be used just on routes after the session is created (for example, after the initial questionnaire).
        * The ``/submit`` path is where the value of ``session['currentUrl']`` is set.
    """
    @wraps(f)
    def decorated_function(*args, **kwargs):
        # The user shouldn't be here yet, redirect to the start
        if not 'currentUrl' in session:
            return redirect('/')

        if 'participantID' in session:
            participant = db.Participant.query.get(session['participantID'])

            # See if the user exists in the database
            if participant is None:
                session.clear()
                return redirect('/')
            # See that the user's IP address matches what's in the database
            if participant.ipAddress != request.environ['REMOTE_ADDR']:
                session.clear()
                return redirect('/')

        return f(*args, **kwargs)
    return decorated_function


# Used to mark functions as handlers for the /submit route. Path is the path specified in ``PAGE_LIST``.
def submit_handler(path):
    """
    A decorator which allows the ``/submit`` route to know which code to execute in response to a specific referrer.

    For example, if there were a form on the ``/myForm`` route/view which POSTed to ``/submit``, you could use
    the ``submitHandler`` decorator to define code which should execute after the user submits the form.
    ::
        @submitHandler("myForm")
        def handleMyForm():
            doStuffToHandleTheForm()

    :param str path: The path to handle.

    .. note::
        You need to ensure that your function is imported at run-time. This is best done when your app is created.
        For example, in the root __init__.py file, ::
            from blueprints.survey_mturk.submit_handlers import *
    """

    def decorator(f):
        print "Submit handler added: " + path
        #handle_pages[path] = f
        current_app.handle_pages[path] = f

        @wraps(f)
        def decorated_function(*args, **kwargs):
            return f(*args, **kwargs)
        return decorated_function
    return decorator


def init_function(f):
    """
    A decorator which allows the user to specify functions to run in the /init route.

    .. note::
        You need to ensure that your function is imported at run-time. This is best done when your app is created.
        For example, in the root __init__.py file, ::
            from blueprints.survey_mturk.init import *
    """

    current_app.init_functions.append(f)

    @wraps(f)
    def decorated_function(*args, **kwargs):
        return f(*args, **kwargs)
    return decorated_function


def page_list_index(path):
    """
    This function determines which index a path is within the ``flat_page_list()`` list.
    :param str path: the path to determine the index of.
    :returns: int -- the index of the path

    .. note::
        * Uses startswith() to determine a match.
        * Paths will have their leading forward-slash removed, if it exists.
    """
    if path.startswith("/"):
        path = path[1:]
    for i, page in enumerate(flat_page_list()):
        if page['path'] == path:
            return i
    return None


def next_path_in_list(current_path=None):
    """
    Gives the next path from ``flat_page_list()``, based on incrementing the index of the current path.

    :param str current_path: The user's current path
    :returns: str -- the next path in ``flat_page_list()`` which the user should be sent to.
    """
    if current_path is None:
        current_path = request.path
    if current_path.startswith("/"):
        path = current_path[1:]
    currentIndex = page_list_index(current_path)

    return flat_page_list()[currentIndex + 1]['path']


def previous_path_in_list(current_path=None):
    """
    Gives the previous path from ``flat_page_list()``, based on incrementing the index of the current path.

    :param str current_path: The user's current path
    :returns: str -- the next path in ``flat_page_list()`` which the user should be sent to.
    """
    if current_path is None:
        current_path = request.path
    if current_path.startswith("/"):
        path = current_path[1:]
    currentIndex = page_list_index(current_path)

    return flat_page_list()[currentIndex - 1]['path']
    

def redirect_and_set_next_path(current_path=None):
    """
    Uses the next_path_in_list() method to redirect the user
    
    :param str current_path: The user's current path
    :returns: str -- the next path in PAGE_LIST which the user should be sent to.
    """
    session['currentUrl'] = next_path_in_list(current_path)
    return redirect(current_app.config["APPLICATION_ROOT"] + "/" + session['currentUrl'])


def create_breadcrumbs():
    """
    An optional function, the result of which can be passed to templates which extend the base ``template.html`` file.
    Pages with the same name will be represented as Page Name (3) or **Page Name (2 of 3)** when the user is on that
    particular page.

    :returns: A list of "breadcrumbs", each of which are a dictionary with a human-readable name for the path, and
     whether or not that page is the active page, meaning it should be made bold.
    """

    page_list = flat_page_list()
    currentIndex = page_list_index(request.path)
    crumbs = []

    # Create breadcrumbs (duplicates handled no differently than anything else)
    for i, page in enumerate(page_list):
        crumb = {'name': page['name'], 'active': False}

        if page_list.index(page) == currentIndex:
            crumb['active'] = True

        crumbs.append(crumb)

    # Check for and handle any groupings of pages with the same name.
    for i, crumb in enumerate(crumbs):
        if i+1 == len(crumbs):
            break

        crumbsInGroup = 1
        positionInGroup = 0

        if crumb['active']:
            positionInGroup = crumbsInGroup

        # Keep removing pages after the first one which have the same name.
        while crumbs[i]['name'] == crumbs[i+1]['name']:
            removedCrumb = crumbs.pop(i+1)

            crumbsInGroup += 1

            if removedCrumb['active']:
                crumbs[i]['active'] = True
                positionInGroup = crumbsInGroup

        if crumbsInGroup > 1 and positionInGroup > 0:
            crumbs[i]['name'] += str.format(" ({0} of {1})", positionInGroup, crumbsInGroup)
        elif crumbsInGroup > 1:
            crumbs[i]['name'] += str.format(" ({0})", crumbsInGroup)

    return crumbs


def get_questionnaire_list():
    """
    Returns a list of the questionnaires specified in the config's PAGE_LIST variable.
    """
    # TODO: Consider using this in load_questionnaires() in BOFSFlask

    questionnaires = []

    for page in flat_page_list():
        if not page['path'].startswith("questionnaire/") and not page['path'].startswith("spa"):
            continue

        if page['path'].startswith("spa"):
            for p2 in page['ajax_paths']:
                if not p2.startswith("questionnaire_ajax/"):
                    continue

                questionnaires.append(p2.replace("questionnaire_ajax/", "", 1))
            continue

        questionnaires.append(page['path'].replace("questionnaire/", "", 1))

    return questionnaires


def get_untagged_questionnaire_list():
    """
    Returns a list of the questionnaires specified in the config's PAGE_LIST variable.
    This also removes the tags and avoids duplicate entries
    """

    questionnaires = get_questionnaire_list()
    untaggedQuestionnaires = list()

    for q in questionnaires:
        qName = q.split('/')[0]

        # Avoid duplicates by only adding questionnaires not in the list already.
        if not qName in untaggedQuestionnaires:
            untaggedQuestionnaires.append(qName)

    return untaggedQuestionnaires

def fetch_attr(obj, attribute, *args):
    """
    Returns attribute value, or calls a method. Can handle attributes nested with dots (.)

    This is similar to Python's built in [https://docs.python.org/2/library/functions.html#getattr getattr()],
    but with support for an arbitrary depth.
    """
    try:
        getAttr = operator.attrgetter(attribute)
        attr = getAttr(obj)
    except AttributeError as e:
        return None

    if callable(attr):
        return attr()
    else:
        return attr


def flat_page_list(condition=None):
    """
    All references to current_app.config['PAGE_LIST'] should instead use this method, unless direct access is required.
    By default, it tries to get the current condition from the session variable.
    :param condition: Set this to override the default functionality
    :return:
    """
    page_list = current_app.config['PAGE_LIST']


    try:
        # If session exists, and condition is a key, then let's grab the current condition!
        if condition is None and not session is None and 'condition' in session:
            condition = session['condition']
            if condition == -1:
                condition = 0
    except:
        pass  # This is almost definitely a "Working outside of request context" error

    # If that doesn't work, default to 0
    if condition is None:
        condition = 0

    flat_page_list = list()

    for entry in page_list:
        if 'conditional_routing' in entry:
            for conditional_route in entry['conditional_routing']:
                # The default operation, if condition is 0, is to just take the first entry
                # This ensures backwards compatibility with the old code so I don't need to update it it all at once.
                # otherwise, look for a match
                if condition == 0 or conditional_route['condition'] == condition:
                    for conditional_entry in conditional_route['page_list']:
                        flat_page_list.append(conditional_entry)
                    break  # once a match has been found, then we're done
        else:
            flat_page_list.append(entry)

    return flat_page_list


def fetch_condition_count():
    return db.session.query(db.func.max(db.Participant.condition)).one()[0]