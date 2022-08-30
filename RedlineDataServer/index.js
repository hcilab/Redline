const server = require('server');
const mongoose = require('mongoose');
const compression = require('compression');
const fs = require('fs');
const _ = require('lodash');
const levelConfigs = require('public/levels.json');
const playerConfig = require('public/player.json');
const defaultConfig = require('public/defaultLevel.json');
const setConfigs = require('public/sets.js');
const { get, post, put, error } = server.router;
const { render, json, status, header } = server.reply;

var redline_entry_schema =  mongoose.Schema({
    date: { type: Date, default: Date.now }
  , time: Number
  , counter: Number
  , mturk_id: Number
  , id: { type: Number }
  , trial: Number
  , set: Number
  , level: Number
  , hp: Number
  , bar: String
  , damage: Number
  , score: Number
  , proximity: Number
  , avg_intensity_in_proximity: Number
  , active: Number
  , distance: Number
  , waterUsed: Number
  , type: String
  , fps: Number
});

let uri = process.env.MONGO_URI || "mongodb://localhost:27017";
console.log("connecting to mongo db: " + uri);
mongoose.connect( uri );

// mongoose.connect("mongodb://localhost/redline");
var db = mongoose.connection;
const entry_model = db.model( 'atomic_entries', redline_entry_schema );
const final_model = db.model( 'cumulative_entries', redline_entry_schema );

const corsExpress = require('cors')({
  // origin: /.*\.cs\.unb\.ca$/
    origin: /.*redline-server\.herokuapp\.com$/
});

const cors = server.utils.modern(corsExpress);

const comp = server.utils.modern(compression);

server(
  {
    security: {
      csrf: false
    },
    views: '../BOFS/app/redline/templates',
    public: '../BOFS/app/redline/public'
  },
  cors,
  [
    get( '/', ctx => render('index.html') )
  , get('/id', async ctx => {
    let id = -1;
    await generateID( 0 ).then(
      gid => id = gid
    );
    if( id != -1 ) return status(200).send( { "id": id } );
    return status(500).send("Error generating new session ID");

  })
  , get('/bar', async ctx => {
    let bar = 1;
    let totalCount = 0;

    await final_model.count().then( count => {
      totalCount = count;
    });

    await final_model.count( {'bar': 'Passive' } ).then( count => {
      ctx.log.debug( count + " out of " + totalCount + " entries using passive music");
      bar = count % 2;
    }).catch( () => {
      return status(500).send("An error occured allocating a music type.");
    });
    ctx.log.debug("sending music: " + bar );

    return status(200).send( { "bar": bar } );
  })
  , get('/config/:resource', async ctx => {
    ctx.log.debug( "Processing resource request for " + ctx.params.resource );

    var setNumber;
    if( !_.isNil( ctx.query.set ) ) {
      try {
        setNumber = parseInt( ctx.query.set );
        ctx.log.debug( "Recieved set number " + setNumber );
      } catch( err ) {
        return send(400).send( "Invalid set count request.<br>" + err );
      }
    }

    if( ctx.params.resource == "player" ) {
      return status(200).send( playerConfig );
    } else if ( ctx.params.resource == "levelCount" ) {
      //if a set is supplied return the number of levels in that set
      if( _.isNumber(setNumber) && setConfigs != null ) {
            return status(200).send({
              "count": setConfigs[setNumber].length
            });
      }

      //return the total number of levels by default
      if( levelConfigs != null ) return status(200).send(
        {
          "count": levelConfigs.length
        });
    } else {
      var levelNumber;
      console.log("attempting to load level")

      try {
        levelNumber = parseInt( ctx.params.resource ) - 1;
      } catch (err) {
        return status(400).send( "Invalid resource request. Cannot parse level number." + err);
      }

      if( !isNaN(levelNumber)
          && levelNumber < levelConfigs.length
          && levelNumber >= 0
        ) {
          var levelToLoad = levelNumber;
          if( _.isNumber(setNumber) && setConfigs != null && setNumber < setConfigs.length )
            levelToLoad = setConfigs[setNumber][levelNumber];

          ctx.log.debug( "Sending level " + levelToLoad );

          let levelConfig = _.defaultsDeep(
            levelConfigs[ levelToLoad ],
            defaultConfig );
          return status(200).send( levelConfig );
        }
    }
    return status(400).send("Invalid resource request. Cannot find resource.");
  })
  , get('/trial/:id', async ctx => {
    let trialNumber = -1;
    await final_model.count( { 'id': ctx.params.id } ).then( count => {
      trialNumber = count + 1;
    });
    if( trialNumber != -1 ) return status(200).send( { trial: trialNumber } );
    return status( 500 ).send("cannot determine trial number");
  })
  , post('/', async ctx => {
    ctx.log.debug( ctx.data );

    await validateTrialNumber(ctx.data.id, ctx.data.trial).then(
      () => {},
      (err) => { return status(403).send(err); }
    )

    const entry = new entry_model( ctx.data );
    await entry.save();
    ctx.log.info('ATOMIC ENTRY ' + ctx.data.id
    + ' TRIAL '
    + entry.trial
    + ' '
    + ctx.data.level );
    ctx.log.debug(ctx.data);

    return status(200).send("data successfully logged");
  })
  , post('/bulk/', async ctx => {
    ctx.log.debug( ctx.data.length );

    await validateTrialNumber( ctx.data[0].id, ctx.data[0].trial ).then(
      () => {},
      ( err ) => { return status(403).send(err); }
    );

    for( let i = 0; i < ctx.data.length; i++ ) {
      const entry = new entry_model( ctx.data[i] );
      await entry.save();
      ctx.log.info('ATOMIC ENTRY ' + ctx.data[i].id
      + ' TRIAL '
      + entry.trial
      + ' '
      + ctx.data.level );
    }

    return status(200).send("data successfully logged");
  })
  , post('/final/', async ctx => {
    ctx.log.debug( ctx.data );

    await final_model.count( { 'id': ctx.data.id } ).then( count => {
      if( count + 1 != ctx.data.trial )
        return status(403).send("Trial number mismatch");
    });

    const entry = new final_model( ctx.data );
    await entry.save();
    ctx.log.info(
      'FINAL ENTRY ' + ctx.data.id
    + ' TRIAL ' + entry.trial
    + ' BAR ' + ctx.data.bar
    + ' LEVEL ' + ctx.data.level
    + ' ' + ctx.data.type );
    ctx.log.debug( ctx.data );
    return status(200).send("data successfully logged");
  })
  , post('/invalidate/', async ctx => {
    ctx.log.info(
      "Invalidating " + ctx.data.id + " trial " + ctx.data.trial );
    const query = {
      id: ctx.data.id,
      trial: ctx.data.trial
    };
    const update = { $set: { type: "INVALID" } };

    await validate( query ).then( () => {
      return status(200);
    }, err => {
      return status(500).send(err);
    });
  })
  , error( ctx => {
    ctx.log.error( ctx.error.message );
    return status(500).send(ctx.error.message);
  })
  , ctx => status(404)
]).then(ctx => {
  ctx.log.debug('DEBUG ENABLED')
  ctx.log.info(`Server launched on http://localhost:${ctx.options.port}/`);
});

function validateTrialNumber( id, trial ) {
  return new Promise( (resolve, reject) => {
    final_model.count( { 'id': ctx.data.id } ).then( count => {
      if( count + 1 != ctx.data.trial )
        reject("Trial number mismatch");
      resolve();
    });
  });
}

function generateID( counter ) {
  return new Promise( (resolve, reject) => {
    if( counter > 100 ) reject();
    let randomID = 0;
    randomID = ( "000000" + (Math.random() * 10000 + 1).toFixed(0) ).slice(-6);
    final_model.count( { 'id': randomID }, function (err, count) {
        console.debug( "count for " + randomID + " is " + count );
        if( count != 0 ) generateID( ++counter );
        else resolve( randomID );
    });
  });
}

function trialValidation() {
  var thresholdDate = new Date();
  thresholdDate.setDate( thresholdDate.getDate() - 1 );
  console.log( thresholdDate );
  entry_model.find(
    {
      date: { $lt: thresholdDate }
      , type: { $ne: "INVALID" }
    } ).then( ( docs, err ) => {
      if( _.isNil(docs) ) {
        console.log( "No entries found that qualify for validation." );
        return;
      }
      console.log( "Reducing " + docs.length + " documents for validation." );
      var result = [];
      _.reduce( docs, ( result, item ) => {
        var entry = _.find( result, n => {
          if( !_.isNil(item) && !_.isNil(n) ) return n;
        });

        if( _.isNil( entry ) ) {
          entry = {
            id: item.id,
            trial: item.trial
          };

          _.concat( result, entry );
        }
      });

      console.log( "Validating " + result.length + " trials." );
      _.forEach( result, item => {
        const q = {
          id: item.id,
          trial: item.trial
        };

        validate( q );
      });
    });
}

function validate( query ) {
  const action = { $set: { type: "INVALID" } };
  return new Promise( (resolve, reject ) => {
    final_model.count( query ).then( count => {
      if( count == 0 )
        entry_model.updateMany( query, action ).exec();
      resolve();
    }, reject );
  });
}
