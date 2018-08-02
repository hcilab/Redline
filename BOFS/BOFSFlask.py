from flask import Flask, render_template, request, session, redirect, send_from_directory
from flask_sqlalchemy import SQLAlchemy
from flask.config import Config
import jinja2
import sys
import os
import util
from datetime import datetime
from JSONQuestionnaire import JSONQuestionnaire
from globals import referrer
from flask_socketio import SocketIO
import urllib


class BOFSFlask(Flask):
    def __init__(self, import_name, use_socketio=False, root_path=None):
        super(BOFSFlask, self).__init__(import_name)
        if root_path:
            self.root_path = root_path

        self.config = self.make_config()
        self.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False
        if(os.environ.get("DATABASE_URL") is not None):
            self.config["SQLALCHEMY_DATABASE_URI"] = os.environ.get("DATABASE_URL")

        bofs_path = os.path.dirname(os.path.abspath(__file__))
        #bofs_path = bofs_path.replace("\\BOFS", "").replace("/BOFS", "")

        self.bofs_path = bofs_path
        #self.config = Config(self.bofs_path, self.default_config)

        self.db = SQLAlchemy(self)
        self.db_tables = []
        self.questionnaires = {}
        self.handle_pages = {}
        self.init_functions = []

        # TODO: Remove use_socketio and instead just use debug

        self.use_socketio = use_socketio

        if use_socketio:
            self.socketio = SocketIO(self, async_mode='eventlet')

        self.add_url_rule("/submit", endpoint="submit", view_func=self.routes_submit, methods=['POST'])
        self.add_url_rule("/BOFS_static/<path:filename>", endpoint="BOFS_static", view_func=self.route_BOFS_static)
        self.add_url_rule("/JSON_questionnaire/<path:filename>", endpoint="JSON_questionnaire", view_func=self.route_JSON_questionnaire)
        self.add_url_rule("/restart", endpoint="route_restart", view_func=self.route_restart)
        self.register_error_handler(404, self.page_not_found)


        my_loader = jinja2.ChoiceLoader([
            self.jinja_loader,
            jinja2.FileSystemLoader(self.bofs_path + '/templates'),
        ])

        self.jinja_loader = my_loader

    # Overriding this ensures compatibility with existing run.py files regardless of whether socketio is used or not
    def run(self, host=None, port=None, debug=None, **options):
        if not self.use_socketio:
            print('\033[91m' + '\033[1m') # Red
            print("!!!!!!!!!!!!!!!!!!!!!!!!!!!!! WARNING !!!!!!!!!!!!!!!!!!!!!!!!!!!!!")
            print(" Flask's built in web server should be used for development ONLY! ")
            print(" If you are deploying online (e.g. MTurk), ensure that you have ")
            print(" set use_socketio=True, and app.debug=False")
            print("!!!!!!!!!!!!!!!!!!!!!!!!!!!!! WARNING !!!!!!!!!!!!!!!!!!!!!!!!!!!!!")
            print('\033[0m')  # Red

            super(BOFSFlask, self).run(host, port, debug, **options)
        else:
            if debug == True:
                self.debug = True
            self.socketio.run(self, host=host, port=port, **options)

    def load_config(self, filename, silent=False):
        self.config.from_pyfile(filename, silent=silent)

    def load_blueprint(self, blueprint_path, blueprint_name):
        blueprint = __import__(blueprint_path + ".views", fromlist="views")
        blueprint_var = getattr(blueprint, blueprint_name)
        self.register_blueprint(blueprint_var)

    def load_models(self, blueprint_path):
        # TODO: This should first check for the existence of the models file
        module = __import__(blueprint_path + ".models", fromlist="models")
        create_function = getattr(module, "create")
        my_classes = create_function(self.db)

        if hasattr(my_classes, '__iter__'):  # A list or tuple was returned
            for c in my_classes:
                setattr(self.db, c.__name__, c)
        else:
            setattr(self.db, my_classes.__name__, my_classes)

    def load_submit_handlers(self, blueprint_path):
        # TODO: This should first check for the existence of the submit_handlers file
        __import__(blueprint_path + ".submit_handlers", fromlist="submit_handlers")
        del sys.modules[blueprint_path + ".submit_handlers"]

    def load_init_functions(self, blueprint_path):
        # TODO: This should first check for the existence of the init file
        __import__(blueprint_path + ".init", fromlist="init")
        del sys.modules[blueprint_path + ".init"]

    def load_questionnaire(self, filename, add_to_db=False):
        if "questionnaire_" + filename in self.db.metadata.tables:
            return

        if filename in self.questionnaires:
            return

        questionnaire = JSONQuestionnaire(filename)
        questionnaire.createDBClass()

        if add_to_db:
            setattr(self.db, "Questionnaire" + questionnaire.dbClass.__name__, questionnaire.dbClass)

        self.questionnaires[filename] = questionnaire
        return questionnaire

    def load_questionnaires(self, add_to_db=False):
        # TODO: When using conditional routing, all questionnaires must be declared within the first condition
        # A simple workaround right now is putting questionnaires after /end in the list
        for page in util.flat_page_list():
            if not page['path'].startswith("questionnaire/") and not page['path'].startswith("spa"):
                continue

            if page['path'].startswith("spa"):
                for p2 in page['ajax_paths']:
                    if not p2.startswith("questionnaire_ajax/"):
                        continue

                    p2Parts = p2.split("/")
                    self.load_questionnaire(p2Parts[1], add_to_db)
                continue

            pathParts = page['path'].split("/")
            filename = pathParts[1]
            self.load_questionnaire(filename, add_to_db)

    # Routes
    def routes_submit(self):
        # Handle the various pages defined with @submitHandler(k)
        for k, f in self.handle_pages.items():
            if referrer == k:
                f()

        referrering_url = urllib.unquote(referrer).decode('utf8')  # Handle questionnaires with spaces, etc.

        if referrering_url.startswith("questionnaire/"):

            tag = 0
            stringParts = referrering_url.split("/")

            if len(stringParts) == 3:
                tag = stringParts[2]

            q = self.questionnaires[stringParts[1]]
            q.handleQuestionnaire(tag)

        if 'ajax_path' in session and session['ajax_path'].startswith("questionnaire_ajax/"):
            tag = 0
            stringParts = session['ajax_path'].split("/")

            if len(stringParts) == 3:
                tag = stringParts[2]

            q = self.questionnaires[stringParts[1]]
            q.handleQuestionnaire(tag)

            return ""

        session['currentUrl'] = util.next_path_in_list(referrering_url)
        #nextUrl = url_for(session['currentUrl'])
        nextUrl = self.config["APPLICATION_ROOT"] + "/" + session['currentUrl']

        #if not 'participantID' in session and not (referrer == "" or referrer == "consent" or referrer == "start"):
        #   return "Your session has expired. Please restart from the beginning."

        return redirect(nextUrl)

    def route_BOFS_static(self, filename):
        return send_from_directory(self.bofs_path + '/static', filename)

    def route_JSON_questionnaire(self, filename):
        return send_from_directory(self.root_path + '/questionnaires', filename)

    def route_restart(self):
        session.clear()
        return redirect("/")

    def page_not_found(self, e):
        return "Could not load the requested page. If you are just starting out, " \
               "please click <a href=\"restart\"><b>here</b></a> to reset your cookies for this page. " \
               "If that doesn't work, please clear your cookies or switch web browsers.", 404