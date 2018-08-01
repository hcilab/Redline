import os
from BOFSFlask import BOFSFlask


def create_app(path, config_name='default.cfg', use_socketio=False, use_admin=True):
    app = BOFSFlask(__name__, root_path=path, use_socketio=use_socketio)
    app.load_config(config_name, silent=False)

    if 'USE_ADMIN' not in app.config or app.config['USE_ADMIN'] == True:
        app.load_blueprint('BOFS.admin', 'admin')

    if 'BLUEPRINTS' in app.config:
        for bp in app.config['BLUEPRINTS']:
            app.load_blueprint(bp['package'], bp['name'])

            if 'has_models' in bp and bp['has_models']:
                app.load_models(bp['package'])

            app_context = app.app_context()
            app_context.push()

            if 'has_submit_handlers' in bp and bp['has_submit_handlers']:
                app.load_submit_handlers(bp['package'])
            if 'has_init' in bp and bp['has_init']:
                app.load_init_functions(bp['package'])

            app_context.pop()

    app.load_blueprint('BOFS.default', 'default')
    app.load_models('BOFS.default')

    # Set defaults for USE_LOGO and USE_BREADCRUMBS
    if not 'USE_BREADCRUMBS' in app.config:
        app.config['USE_BREADCRUMBS'] = True

    if not 'USE_LOGO' in app.config:
        app.config['USE_LOGO'] = True

    with app.app_context():
        app.load_submit_handlers('BOFS.default')
        app.load_questionnaires()

    app.db.create_all()

    return app