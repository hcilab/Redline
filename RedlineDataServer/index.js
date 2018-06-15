const server = require('server');
const mongoose = require('mongoose');
const compression = require('compression');
const fs = require('fs');
const _ = require('lodash');
const levelConfigs = require('public/levels.json');
const playerConfig = require('public/player.json');
const defaultConfig = require('public/defaultLevel.json');
const { get, post, put, error } = server.router;
const { render, json, status, header } = server.reply;

var redline_entry_schema =  mongoose.Schema({
    date: { type: Date, default: Date.now }
  , time: String
  , counter: String
  , id: { type: Number }
  , trial: Number
  , level: String
  , hp: Number
  , bar: String
  , damage: Number
  , score: Number
  , proximity: Number
  , avg_intensity_in_proximity: Number
  , active: Number
  , type: String
  , fps: Number
});

var uri = "mongodb://admin:URXEBCt5jyU6@cluster0-shard-00-00-y246y.mongodb.net:27017,cluster0-shard-00-01-y246y.mongodb.net:27017,cluster0-shard-00-02-y246y.mongodb.net:27017/redline?ssl=true&replicaSet=Cluster0-shard-0&authSource=admin";
mongoose.connect(uri);

// mongoose.connect("mongodb://localhost/redline");
var db = mongoose.connection;
const entry_model = db.model( 'atomic_entries', redline_entry_schema );
const final_model = db.model( 'cumulative_entries', redline_entry_schema );

let tableData = {
  atomic_entries: [],
  cumulative_entries: []

};

const corsExpress = require('cors')({
  origin: ['http://hcidev.cs.unb.ca', 'https://hcidev.cs.unb.ca']
});

const cors = server.utils.modern(corsExpress);

const comp = server.utils.modern(compression);

server(
  { log: 'debug', 
    security: {
      csrf: false
    }
  },
  cors,
  [
    get( '/', ctx => {
      return render("index.html");
    })
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

    await final_model.count( {'bar': 'Linear HP Bar' } ).then( count => {
      ctx.log.debug( count + " out of " + totalCount + " entries use the Linear bar");
      bar = count % 2;
    }).catch( () => {
      return status(500).send("An error occured allocating a bar type.");
    });
    ctx.log.debug("sending bar: " + bar );

    return status(200).send( { "bar": bar } );
  })
  , get('/config/:resource', async ctx => {
    ctx.log.debug( "Processing resource request for " + ctx.params.resource );

    if( ctx.params.resource == "player" ) {
      return status(200).send( playerConfig );
    } else if ( ctx.params.resource == "levelCount" ) {
      if( levelConfigs != null ) return status(200).send(
        {
          "count": levelConfigs.length
        });
    } else {
      var levelNumber;
      try {
        levelNumber = parseInt( ctx.params.resource ) - 1;
      } catch (err) {
        return status(400).send( "Invalid resource request.<br>" + err);
      }
      if( !isNaN(levelNumber)
          && levelNumber < levelConfigs.length
          && levelNumber >= 0
        ) {
          let levelConfig = _.defaultsDeep( levelConfigs[ levelNumber ], defaultConfig );
          return status(200).send( levelConfigs[ levelNumber ] );
        }
    }
    return status(400).send("Invalid resource request.");
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

    tableData.atomic_entries.push( ctx.data );
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
      tableData.atomic_entries.push( ctx.data[i] );
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

    tableData.cumulative_entries.push( ctx.data );
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
