from datetime import datetime
from flask import Blueprint, render_template, current_app, request
from BOFS.util import *
from BOFS.globals import db, referrer


default = Blueprint('default', __name__)


@default.route("/")
@verify_correct_page
def index():
    return "You shouldn't be able to see this."


@default.route("/consent")
@verify_correct_page
def consent():
        return render_template('consent.html')


# Sets participant's condition to -1 always
@default.route("/consent_nc")
@verify_correct_page
def consent_nc():
        return render_template('consent.html')


@default.route("/assign_condition")
@verify_session_valid
@verify_correct_page
def assign_condition():
    p = db.session.query(db.Participant).get(session['participantID'])
    p.assign_condition()

    db.session.commit()

    session['condition'] = p.condition

    return redirect("/redirect_next_page")


@default.route("/startMTurk")
@verify_correct_page
def startMTurk():
    return render_template('startMTurk.html', crumbs=create_breadcrumbs())


@default.route("/questionnaire/<questionnaireName>")
@default.route("/questionnaire/<questionnaireName>/<tag>")
@verify_correct_page
def questionnaire(questionnaireName, tag=""):
    return render_template('questionnaire.html',
                           tag=tag,
                           crumbs=create_breadcrumbs(),
                           questionnaireName=questionnaireName,
                           timeStarted=datetime.now())


@default.route("/questionnaire_ajax/<questionnaireName>")
@default.route("/questionnaire_ajax/<questionnaireName>/<tag>")
def questionnaireAjax(questionnaireName, tag=""):
    return render_template('questionnaireAjax.html',
                           tag=tag,
                           crumbs=create_breadcrumbs(),
                           questionnaireName=questionnaireName,
                           timeStarted=datetime.now())


@default.route("/spa")
@verify_correct_page
def singlePageAjax():
    return render_template('singlePageAjax.html')


@default.route("/spa_current")
def spaCurrent():
    if not 'ajax_path' in session:
        currentPageIndex = page_list_index('spa')
        paths = current_app.config['PAGE_LIST'][currentPageIndex]['ajax_paths']

        session['ajax_path'] = paths[0]

    return redirect(session['ajax_path'])


@default.route("/spa_next")
def spaNext():
    currentPageIndex = page_list_index('spa')
    ajaxPaths = flat_page_list()[currentPageIndex]['ajax_paths']

    currentAjaxPathIndex = ajaxPaths.index(session['ajax_path'])

    # When the user reaches the end of the ajax pages, send them to a special page which
    # redirects them to the next page in PAGE_LIST
    if currentAjaxPathIndex + 1 == len(ajaxPaths):
        session['ajax_path'] = "/spa_end"
    else:
        session['ajax_path'] = ajaxPaths[currentAjaxPathIndex + 1]

    return redirect(session['ajax_path'])


@default.route("/spa_end")
def spaEnd():
    return "<script>window.parent.location.assign(\"/redirect_next_page\");</script>"


@default.route("/redirect_previous_page")
def redirectPreviousPage():
    session['currentUrl'] = previous_path_in_list(session['currentUrl'])
    nextUrl = current_app.config["APPLICATION_ROOT"] + "/" + session['currentUrl']

    return redirect(nextUrl)


@default.route("/redirect_next_page")
def redirectNextPage():
    if not request is None and not request.referrer is None:
        currentPage = str.replace(request.referrer, request.host_url, "")
    else:
        currentPage = session['currentUrl']

    if currentPage == "end":
        return redirect(current_app.config["APPLICATION_ROOT"] + "/end")

    session['currentUrl'] = next_path_in_list(currentPage)
    nextUrl = current_app.config["APPLICATION_ROOT"] + "/" + session['currentUrl']

    return redirect(nextUrl)


@default.route("/redirect_next_page/<page>")
def redirectFromPage(page):
    session['currentUrl'] = next_path_in_list(page)
    nextUrl = current_app.config["APPLICATION_ROOT"] + "/" + session['currentUrl']

    return redirect(nextUrl)


@default.route("/hr_start")
@verify_session_valid
@verify_correct_page
def heartrate_start():
    return render_template("heartrate/start.html")


@default.route("/hr_end")
@verify_session_valid
@verify_correct_page
def heartrate_end():
    return render_template("heartrate/end.html")


@default.route("/hr_window")
@verify_session_valid
def heartrate_window():
    return render_template("heartrate/window.html")


@default.route("/hr_submit", methods=['POST'])
def heartrate_submit():

    timeStampUnixList = str(request.form['timestampUnix']).split(",")[:-1]
    bpmList = str(request.form['bpm']).split(",")[:-1]
    fqQualityList = str(request.form['fqQuality']).split(",")[:-1]
    dataQualityList = str(request.form['dataQuality']).split(",")[:-1]

    for i in range(len(bpmList)):
        hr = db.HeartRate()
        hr.participantID = int(session['participantID'])
        hr.timeStampUnix = timeStampUnixList[i]
        hr.heartRate = bpmList[i]
        hr.fqQuality = fqQualityList[i]
        hr.dataQuality = dataQualityList[i]
        hr.url = request.form['url']

        db.session.add(hr)

    db.session.commit()

    return ""


@default.route("/end")
@verify_correct_page
@verify_session_valid
def end():
    p = db.Participant.query.get(session['participantID'])
    p.timeEnded = datetime.now()
    p.finished = True

    db.session.commit()

    return render_template('end.html', code=session['code'])


@default.route("/current_url")
def current_url():
    return session['currentUrl']


"""
@default.route("/submit", methods=['POST'])
def submit():
    # Handle the various pages defined with @submitHandler(k)
    for k, f in current_app.bofs.handle_pages.items():
        if referrer.startswith(k):
            f()

    if referrer.startswith("questionnaire/"):
        q = current_app.bofs.questionnaires[referrer.replace("questionnaire/", "")]
        q.handleQuestionnaire()

    session['currentUrl'] = next_path_in_list(referrer)
    #nextUrl = url_for(session['currentUrl'])
    nextUrl = current_app.config["APPLICATION_ROOT"] + "/" + session['currentUrl']

    #if not 'participantID' in session and not (referrer == "" or referrer == "consent" or referrer == "start"):
    #   return "Your session has expired. Please restart from the beginning."

    return redirect(nextUrl)
"""