from datetime import datetime


def create(db):
    class BejeweledStats(db.Model):
        __tablename__ = "bejeweled_stats"

        bejeweledStatID = db.Column(db.Integer, primary_key=True, autoincrement=True)
        participantID = db.Column(db.Integer, db.ForeignKey('participant.participantID'))
        timeCompleted = db.Column(db.DateTime, nullable=False, default=datetime.now())
        roundNum = db.Column(db.Integer, nullable=False, default=0)
        score = db.Column(db.Integer, nullable=False, default=0)
        row3 = db.Column(db.Integer, nullable=False, default=0)
        row4 = db.Column(db.Integer, nullable=False, default=0)
        row5 = db.Column(db.Integer, nullable=False, default=0)
        special_4 = db.Column(db.Integer, nullable=False, default=0)
        special_5 = db.Column(db.Integer, nullable=False, default=0)

    return BejeweledStats