from datetime import datetime

from flask import Blueprint, render_template

from BOFS.util import *
from BOFS.globals import db, questionnaires

redline = Blueprint('redline', __name__,
                      static_url_path='/redline', template_folder='templates', static_folder='public')


@redline.route("/instructions")
@verify_correct_page
@verify_session_valid
def intro():
    return render_template("instructions.html")

@redline.route("/tutorial")
@verify_correct_page
@verify_session_valid
def tutorial():
    return render_template("tutorial.html")


@redline.route("/game_redline_0")
@verify_correct_page
@verify_session_valid
def game_redline_0():
    PID = session['participantID']
    CONDITION = getCondition()
    SET = 0
    VERSION = get_version_number()
    SEX=getGender()

    return render_template(
        "index.html",
        application_root=current_app.config["APPLICATION_ROOT"],
        PID=PID,
        CONDITION=CONDITION,
        SET=SET,
        VERSION=VERSION,
        SEX=SEX
    )


@redline.route("/game_redline_1")
@verify_correct_page
@verify_session_valid
def game_redline_1():
    PID = session['participantID']
    CONDITION = getCondition()
    SET = 1
    VERSION = get_version_number()
    SEX=getGender()

    return render_template(
        "index.html",
        application_root=current_app.config["APPLICATION_ROOT"],
        PID=PID,
        CONDITION=CONDITION,
        SET=SET,
        VERSION=VERSION,
        SEX=SEX
    )

@redline.route("/game_redline_2")
@verify_correct_page
@verify_session_valid
def game_redline_2():
    PID = session['participantID']
    CONDITION = getCondition()

    SET = 2

    VERSION = get_version_number();

    SEX = getGender();

    return render_template(
        "index.html",
        application_root=current_app.config["APPLICATION_ROOT"],
        PID=PID,
        CONDITION=CONDITION,
        SET=SET,
        VERSION=VERSION,
        SEX=SEX
    )


def get_version_number():
    cmd = "git --git-dir=/home/jwuertz/Redline/.git rev-parse --short HEAD"
    import subprocess
    p = subprocess.Popen(cmd.split(), stdout=subprocess.PIPE)
    output, error = p.communicate()
    return output.strip()

def getDemographicsInfo(participantID):
    return db.session.query(questionnaires["Demographics"].dbClass).filter(questionnaires["Demographics"].dbClass.participantID == participantID).first()

def getGender():
    participantID = session['participantID']

    currentParticipant = db.session.query(db.Participant).get(participantID)
    mTurkID = currentParticipant.mTurkID

    participantInfo = db.session.query(db.Participant).filter(db.Participant.mTurkID == mTurkID).all()#get participantInfo

    avatar_sex = 'female'
    for p in participantInfo:
        demographicsInfo = getDemographicsInfo(p.participantID)
        if demographicsInfo != None:
            gender = demographicsInfo.gender
            avatar_sex = demographicsInfo.representation_sex

    return 1 if avatar_sex == 'male' else 0

def getCondition():
    #get number of participants that have completed condition 1
    condNum = db.session.query(db.Participant).filter(db.Participant.finished == 1).count()
    totalCount = db.session.query(db.Participant).count()
    if(condNum < totalCount/2):
        return 1
    return 0