from datetime import datetime

from flask import Blueprint, render_template

from BOFS.util import *
from BOFS.globals import db

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
    CONDITION = session['condition']
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
    CONDITION = session['condition']
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
    CONDITION = session['condition']

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

def getGender():
    participantID = session['participantID']

    currentParticipant = db.session.query(db.Participant).get(participantID)
    mTurkID = currentParticipant.mTurkID

    participantInfo = db.session.query(db.Participant).filter(db.Participant.mTurkID == mTurkID).all()#get participantInfo

    avatar_sex = 'female'
    for p in participantInfo:
        demographicsInfo = getDemographicsInfo(p.participantID)
        if getDemographicsInfo(p.participantID) != None:
            gender = demographicsInfo.gender
            avatar_sex = demographicsInfo.representation_sex

    return 1 if avatar_sex == 'male' else 0
