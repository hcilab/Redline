import json
from os import listdir, path
from functools import wraps
from flask import request, session, current_app, render_template, g
from BOFS.globals import db
from BOFS.util import get_questionnaire_list, get_untagged_questionnaire_list


def _datetime_convert(v):
    return v.strftime("%Y-%m-%d %H:%M:%S")

def removeNonAscii(s):
    return "".join(filter(lambda x: ord(x)<128, s)).encode('ascii', 'xmlcharrefreplace')

def sqlalchemy_to_json(inst, cls):
    """
    Jsonify the sqlalchemy query result.
    http://stackoverflow.com/questions/7102754/jsonify-a-sqlalchemy-result-set-in-flask
    """
    convert = dict()
    convert['DATETIME'] = _datetime_convert

    d = dict()
    for c in cls.columns:
        v = getattr(inst, c.name)
        if c.type in convert.keys() and v is not None:
            try:
                d[c.name] = convert[c.type](v)
            except Exception:
                d[c.name] = "Error:  Failed to covert using ", str(convert[c.type])
        elif v is None:
            d[c.name] = str()
        else:
            if isinstance(v, unicode):
                d[c.name] = removeNonAscii(v).replace("'", "&#39;").replace("{", "&#123;").replace("}", "&#125;")
            else:
                d[c.name] = str(v)
    return json.dumps(d)


def admin_housekeeping():
    # I don't like this solution. template_admin.html uses this.
    if len(current_app.init_functions) > 0:
        g.hasInitFunctions = True
    else:
        g.hasInitFunctions = False

    # Don't like it, but still can't think of a better way. Used by template_admin.html
    if "ADDITIONAL_ADMIN_PAGES" in current_app.config:
        g.additionalAdminPages = current_app.config['ADDITIONAL_ADMIN_PAGES']
    else:
        g.additionalAdminPages = None

    g.tableNames = []
    for t in db.metadata.tables:
        g.tableNames.append(t)

    g.questionnairesSystem = []

    if path.exists(current_app.root_path + "/questionnaires"):
        for q in listdir(current_app.root_path + "/questionnaires"):
            if q.endswith(".json"):
                g.questionnairesSystem.append(q.replace(".json", ""))

    g.tableNames = sorted(g.tableNames)
    g.questionnairesLive = get_questionnaire_list()
    g.questionnairesLiveUntagged = sorted(get_untagged_questionnaire_list())
    g.questionnairesSystem = sorted(g.questionnairesSystem)


def verify_admin(f):
    """
    A decorator to be used for admin routes, which checks if the user is logged in. If not, the login page is shown.
    """
    @wraps(f)
    def decorated_function(*args, **kwargs):
        if request.method == 'POST':
            if request.form['password'] != current_app.config['ADMIN_PASSWORD']:
                return render_template("login_admin.html", message="The password you entered is incorrect.")
            else:
                session['loggedIn'] = True

        if 'loggedIn' not in session or not session['loggedIn']:
            return render_template("login_admin.html")

        admin_housekeeping()

        return f(*args, **kwargs)
    return decorated_function