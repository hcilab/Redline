from flask import Blueprint, render_template, current_app, redirect, g, request
from BOFS.globals import db, questionnaires
from BOFS.util import get_questionnaire_list, fetch_condition_count
from util import sqlalchemy_to_json, verify_admin
import json
from questionnaireResults import *
from datetime import datetime


admin = Blueprint('admin', __name__, url_prefix='/admin', template_folder='templates', static_folder='static')


@admin.route("/")
def admin_index():
    return redirect("admin/progress")


@admin.route("/progress_ajax")
@verify_admin
def progress_ajax():
    participants = db.session.query(db.Participant).all()
    questionnaires = get_questionnaire_list()
    results = []

    for p in participants:
        result = {}
        result['mTurkID'] = p.mTurkID
        result['participantID'] = p.participantID
        result['condition'] = p.condition
        result['timeStarted'] = ""

        if p.timeStarted.year > 1900:
            result['timeStarted'] = p.timeStarted.strftime("%H:%M:%S")

        result['timeEnded'] = ""

        if p.timeEnded.year > 1900:
            result['timeEnded'] = p.timeEnded.strftime("%H:%M:%S")

        result['duration'] = ((p.timeEnded - p.timeStarted).total_seconds() / 60.0)

        if result['duration'] < 0:
            result['duration'] = ""

        for q in questionnaires:
            tag = ""

            if '/' in q:
                qNameParts = q.split('/')
                qName = qNameParts[0]
                tag = qNameParts[1]
            else:
                qName = q

            qData = p.questionnaire(qName, tag)
            if qData and qData.timeStarted != datetime.min:
                result[q] = (qData.timeEnded - qData.timeStarted).total_seconds()
            else:
                result[q] = ""

        results.append(result)

    return json.dumps(results)


@admin.route("/progress", methods=['GET', 'POST'])
@verify_admin
def progress():
    return render_template("progress.html")


@admin.route("/exportItemTiming", methods=['GET', 'POST'])
@verify_admin
def exportItemTiming():
    questionnaires = get_questionnaire_list()
    header = "participantID,mTurkID"
    output = ""

    headerComplete = False

    results = db.session.query(db.Participant).filter(db.Participant.finished == True).all()

    for p in results:
        output += str.format("{},\"{}\"", p.participantID, p.mTurkID.strip())

        for qName in questionnaires:
            tag = ""

            if '/' in qName:
                qNameParts = qName.split('/')
                qName = qNameParts[0]
                tag = qNameParts[1]

            q = p.questionnaire(qName, tag)
            logs = p.questionnaire_log(qName, tag)

            qNameFull = qName
            if len(tag) > 0:
                qNameFull = "{}_{}".format(qName, tag)

            for key in sorted(logs.iterkeys()):
                if not headerComplete:
                    header += ",{}_{}".format(qNameFull, key)

                output += ",{}".format(logs[key])

        output += "\n"
        headerComplete = True

    return render_template("exportCSV.html", data=str.format("{}\n{}", header, output))


@admin.route("/exportCSV", methods=['GET', 'POST'])
@admin.route("/exportCSV/<all>", methods=['GET', 'POST'])
@verify_admin
def exportCSV(all = False):
    questionnaires = get_questionnaire_list()

    header = "participantID,mTurkID,condition,duration"
    output = ""

    headerComplete = False


    if all:
        results = db.session.query(db.Participant).all()
    else:
        results = db.session.query(db.Participant).filter(db.Participant.finished == True).all()

    for p in results:

        duration = (p.timeEnded - p.timeStarted).total_seconds()
        output += str.format("{},\"{}\",{},{}", p.participantID, p.mTurkID.strip(), p.condition, duration)

        for qName in questionnaires:
            tag = ""

            if '/' in qName:
                qNameParts = qName.split('/')
                qName = qNameParts[0]
                tag = qNameParts[1]

            q = p.questionnaire(qName, tag)

            attr = q.__dict__

            if attr is None:
                continue

            keys = sorted(attr.keys())

            qNameFull = qName
            if len(tag) > 0:
                qNameFull = "{}_{}".format(qName, tag)


            for k in keys:
                if k.startswith("_") or k == 'tag' or k.endswith("ID") or k == 'participant' or k.startswith('time'):
                    continue

                v = attr[k]

                if k.startswith("q_"):
                    k = k.replace("q_", "")

                if k.startswith(qName + "_"):
                    k = k.replace(qName + "_", "")

                if not headerComplete:
                    header += str.format(",{}_{}", qNameFull, k)

                if type(v) is str:
                    v = unicode(v).encode("utf8")
                    output += str.format(",\"{}\"", v.strip().replace("\n", " ").replace("\r", " ").replace("\"", "'"))
                elif type(v) is unicode:
                    v = v.encode("utf8")
                    output += str.format(",\"{}\"", v.strip().replace("\n", " ").replace("\r", " ").replace("\"", "'"))
                else:
                    output += str.format(",{}", v)

            if not headerComplete:
                header += str.format(",{}_{}", qNameFull, "duration")

            if q.timeStarted == datetime.min:
                duration = 0
            else:
                duration = (q.timeEnded - q.timeStarted).total_seconds()
                if duration < 0:
                    duration = "0"

            output += str.format(",{}", duration)


        output += "\n"

        headerComplete = True

    return render_template("exportCSV.html", data=str.format("{}\n{}", header, output))


@admin.route("/previewQuestionnaire/<questionnaireName>", methods=['GET', 'POST'])
@verify_admin
def previewQuestionnaire(questionnaireName):
    errors = []

    try:
        f = open(current_app.root_path + '/questionnaires/' + questionnaireName + ".json", 'r')
        jsonData = f.read()
        json.loads(jsonData)
    except Exception as e:
        errors = list(e.args)

    tableName = "questionnaire_" + questionnaireName

    if questionnaireName in g.questionnairesLive:
        try:
            db.session.query(db.metadata.tables[tableName]).first()
        except Exception as e:
            errors.extend(list(e.args))
            if "(OperationalError) no such column:" in e.args[0]:
                errors.append("Click <a href=\"?fix_errors\">here</a> if you would like to try to automatically add "
                              "this column. Alternatively, you can drop the table and it will be recreated.")
            elif "(OperationalError) no such table:" in e.args[0]:
                errors.append("Click <a href=\"?fix_errors\">here</a> if you would like to try to automatically create "
                              "this table. Alternatively, you can restart the server and it will be created.")

    if 'fix_errors' in request.args:
        # Figure out what column it is by parsing errors.
        for e in errors:
            if "(OperationalError) no such column:" in e:
                e = e.split(tableName + ".")
                columnName = e[len(e)-1]
                dataType = db.metadata.tables[tableName].columns[columnName].type

                addColumn = db.DDL(str.format("ALTER TABLE {} ADD COLUMN {} {}", tableName, columnName, dataType))
                db.engine.execute(addColumn)

                errors.append(str.format("{} {} was added to {}. "
                                         "This error should be gone when you refresh.", columnName, dataType, tableName))

            if "(OperationalError) no such table:" in e:
                db.create_all()
                errors.append(str.format("The error should be gone if you refresh."))

    return render_template("previewQuestionnaire.html", questionnaireName=questionnaireName, errors=errors)


@admin.route("/analyzeQuestionnaire/<questionnaireName>/<tag>", methods=['GET', 'POST'])
@admin.route("/analyzeQuestionnaire/<questionnaireName>", methods=['GET', 'POST'])
@verify_admin
def analyzeQuestionnaire(questionnaireName, tag=0):
    questionnaire = questionnaires[questionnaireName]

    gridPlotData = {}
    gridPlotJSVars = []

    numericResults = NumericResults(questionnaire.dbClass, questionnaire.fields, tag)



    for condition, valueDict in numericResults.dataDescriptive.items():
        gpd = {
            'name': condition,
            'type': 'bar',
            'x': [field for (field, descriptives) in valueDict.items()],
            'y': [descriptives.mean for (field, descriptives) in valueDict.items()],
            'error_y': {
                'type': 'data',
                'visible': True,
                'array': [descriptives.sem for (field, descriptives) in valueDict.items()]
            }
        }
        gridPlotData[condition] = json.dumps(gpd)
        gridPlotJSVars.append("gpd_{}".format(condition))

    return render_template("analyzeQuestionnaire.html",
                           questionnaireName=questionnaireName,
                           tag=tag,
                           conditionCount=fetch_condition_count(),
                           gridPlotData=gridPlotData,
                           gridPlotJSVars=json.dumps(gridPlotJSVars).replace('"', ''),
                           numericResults=numericResults)


@admin.route("/viewTable/<tableName>", methods=['GET', 'POST'])
@verify_admin
def viewTable(tableName):
    rows = None
    try:
        rows = db.session.query(db.metadata.tables[tableName]).all()
    except Exception as e:
        return render_template("viewTable.html", data="", datafields="", columns="", errors=list(e.args))

    datafields = []
    columns = []

    for c in db.metadata.tables[tableName].columns:
        type = str(c.type)
        if type.startswith("VARCHAR") or type.startswith("TEXT"):
            type = "string"

        field = {'name': c.description, 'type': type.lower()}
        column = {'text': c.description, 'datafield': c.description}

        datafields.append(field)
        columns.append(column)

    data = "["

    for r in rows:
        data += sqlalchemy_to_json(r, db.metadata.tables[tableName]) + ","

    data = data[:len(data)-1] + "]"  # Removes the last comma

    if len(data) == 1:
        data = "[]"  # So it fails gracefully.

    return render_template("viewTable.html", data=data, datafields=json.dumps(datafields), columns=json.dumps(columns))


@admin.route("/initialize", methods=['GET', 'POST'])
@verify_admin
def initialize():
    results = []

    for f in current_app.init_functions:
        result = {'function': f.__name__, 'success': True, 'error': ''}
        try:
            f()
        except Exception as e:
            result['success'] = False
            result['error'] = e
            db.session.rollback()

        results.append(result)

    return render_template("initialize.html", results=results)
