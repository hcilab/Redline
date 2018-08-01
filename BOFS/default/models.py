import datetime


def create(db):
    class Participant(db.Model):
        __tablename__ = "participant"

        participantID = db.Column(db.Integer, primary_key=True, autoincrement=True)
        mTurkID = db.Column(db.String(50), nullable=False, default="")
        ipAddress = db.Column(db.String(32), nullable=False, default="")
        userAgent = db.Column(db.String(255), nullable=False, default="")
        condition = db.Column(db.Integer, nullable=False, default=-1)
        timeStarted = db.Column(db.DateTime, nullable=False, default=datetime.datetime.min)  # Starts after consent
        timeEnded = db.Column(db.DateTime, nullable=False, default=datetime.datetime.min)
        finished = db.Column(db.Boolean, nullable=False, default=False)
        code = db.Column(db.String(36), nullable=False, default=0)
        #isTrackingHR = db.Column(db.Boolean, nullable=False, default=False)


        def questionnaire(self, name, tag=""):
            from BOFS.globals import questionnaires
            qResults = getattr(self, "questionnaire_" + name)

            toConsider = []

            for result in qResults:
                if result.tag == tag or (result.tag == u'0' and tag == ''):
                    toConsider.append(result)

            if len(toConsider) == 1:
                return toConsider[0]

            if len(toConsider) > 1:
                mostRecent = None
                for result in toConsider:
                    if mostRecent is None or mostRecent.timeEnded > result.timeEnded:
                        mostRecent = result

                return mostRecent

            return questionnaires[name].createBlank()

        # Return a dictionary of question ID -> time delta
        def questionnaire_log(self, name, tag=""):
            q = self.questionnaire(name, tag)

            if tag == "":
                tag = 0

            logs = db.session.query(db.RadioGridLog).filter(
                db.RadioGridLog.participantID == self.participantID,
                db.RadioGridLog.questionnaire == name,
                db.RadioGridLog.tag == tag
            ).order_by(db.RadioGridLog.timeClicked).all()

            result = {}

            prevTime = q.timeStarted

            for log in logs:
                deltaTime = (log.timeClicked - prevTime).total_seconds()
                prevTime = log.timeClicked

                result[log.questionID] = deltaTime

            return result

        def assign_condition(self):
            from flask import current_app

            if 'CONDITIONS_NUM' in current_app.config and current_app.config['CONDITIONS_NUM'] > 0:
                numConditions = current_app.config['CONDITIONS_NUM']
                pCount = [0] * numConditions

                lowest = None

                printText = "Total conditions: {}, Counts: ".format(numConditions)

                for condition in range(0, numConditions):
                    pCount[condition] = db.session.query(db.Participant).filter(
                        db.Participant.condition == condition).count()
                    if lowest is None or pCount[condition] < lowest:
                        lowest = pCount[condition]

                    printText += "{}, ".format(pCount[condition])

                self.condition = pCount.index(lowest)

                printText += "User put in condition {}.".format(self.condition)
                print printText
            else:
                self.condition = -1


    class PageProgress(db.Model):
        __tablename__ = "page_progress"

        pageProgressID = db.Column(db.Integer, primary_key=True, autoincrement=True)
        participantID = db.Column(db.Integer, db.ForeignKey('participant.participantID'))
        timeStarted = db.Column(db.DateTime, nullable=False, default=datetime.datetime.min)
        timeEnded = db.Column(db.DateTime, nullable=False, default=datetime.datetime.min)


    class RadioGridLog(db.Model):
        __tablename__ = "radio_grid_log"

        radioGridLog = db.Column(db.Integer, primary_key=True, autoincrement=True)
        participantID = db.Column(db.Integer, db.ForeignKey('participant.participantID'))
        timeClicked = db.Column(db.DateTime, nullable=False, default=datetime.datetime.min)
        questionnaire = db.Column(db.String, nullable=False, default="")
        tag = db.Column(db.String, nullable=False, default="")
        questionID = db.Column(db.String, nullable=False, default="")
        value = db.Column(db.String, nullable=False, default="")


    class MatchStatus(db.Model):
        __tablename__ = "match_status"

        matchStatusID = db.Column(db.Integer, primary_key=True, autoincrement=True)
        participantID = db.Column(db.Integer, db.ForeignKey('participant.participantID'))


    class HeartRate(db.Model):
        __tablename__ = "heart_rate"

        heartRateID = db.Column(db.Integer, primary_key=True, autoincrement=True)
        participantID = db.Column(db.Integer, db.ForeignKey('participant.participantID'))
        timeStampUnix = db.Column(db.Integer, nullable=False, default=0)
        heartRate = db.Column(db.Float, nullable=False, default=0.0)
        fqQuality = db.Column(db.Float, nullable=False, default=0.0)
        dataQuality = db.Column(db.Float, nullable=False, default=0.0)
        url = db.Column(db.String(255), nullable=False, default="")


    class Display(db.Model):
        __tablename__ = "display"

        logDisplayID = db.Column(db.Integer, primary_key=True, autoincrement=True)
        participantID = db.Column(db.Integer, db.ForeignKey('participant.participantID'))
        dppx = db.Column(db.Float, nullable=False, default=0.0)
        screenWidth = db.Column(db.Integer, nullable=False, default=0)
        screenHeight = db.Column(db.Integer, nullable=False, default=0)
        innerWidth = db.Column(db.Integer, nullable=False, default=0)
        innerHeight = db.Column(db.Integer, nullable=False, default=0)


    return Participant, RadioGridLog, MatchStatus, HeartRate, Display