from BOFS.util import submit_handler


@submit_handler("consent")
def handle_consent():
    from flask import request, session, current_app
    from BOFS.globals import db
    import datetime

    p = db.Participant()
    p.ipAddress = request.environ['REMOTE_ADDR']  # request.headers.get('X-Real-IP')  # request.remote_addr
    p.userAgent = request.user_agent.string
    p.timeStarted = datetime.datetime.now()
    p.assign_condition()
    db.session.add(p)
    db.session.commit()

    session['participantID'] = p.participantID
    session['condition'] = p.condition
    session['code'] = p.code

    entry = db.Display()
    entry.participantID = session['participantID']
    entry.dppx = request.form['dppx']
    entry.screenWidth = request.form['screenWidth']
    entry.screenHeight = request.form['screenHeight']
    entry.innerWidth = request.form['innerWidth']
    entry.innerHeight = request.form['innerHeight']

    db.session.add(entry)
    db.session.commit()


@submit_handler("consent_nc")
def handle_consent_nc():
    from flask import request, session, current_app
    from BOFS.globals import db
    import datetime

    p = db.Participant()
    p.ipAddress = request.environ['REMOTE_ADDR']  # request.headers.get('X-Real-IP')  # request.remote_addr
    p.userAgent = request.user_agent.string
    p.timeStarted = datetime.datetime.now()
    p.condition = -1

    db.session.add(p)
    db.session.commit()

    session['participantID'] = p.participantID
    session['condition'] = p.condition
    session['code'] = p.code

    entry = db.Display()
    entry.participantID = session['participantID']
    entry.dppx = request.form['dppx']
    entry.screenWidth = request.form['screenWidth']
    entry.screenHeight = request.form['screenHeight']
    entry.innerWidth = request.form['innerWidth']
    entry.innerHeight = request.form['innerHeight']

    db.session.add(entry)
    db.session.commit()


@submit_handler("startMTurk")
def handleStartMTurk():
    from flask import session, request
    from BOFS.globals import db
    import uuid

    p = db.Participant.query.get(session['participantID'])
    p.mTurkID = str(request.form['mTurkID']).strip()
    p.code = uuid.uuid4().hex

    session['code'] = p.code

    db.session.commit()