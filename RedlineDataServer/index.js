const server = require('server');
const mongoose = require('mongoose');
const compression = require('compression');
const fs = require('fs');
const levelConfigs = require('public/levels.json');
const playerConfig = require('public/player.json');
const { get, post, put, error } = server.router;
const { render, json, status, header } = server.reply;

var redline_entry_schema =  mongoose.Schema({
    date: { type: Date, default: Date.now }
  , time: String
  , counter: String
  , id: { type: Number, unique: true }
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
  {
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

    await final_model.count( {'bar': 'Offset HP Bar' } ).then( count => {
      ctx.log.debug( "There are currently " + count + " Offset entries");
      if( count > totalCount/2 ) bar = 0;
      else bar = count % 2;
    }).catch( () => {
      return status(500).send("An error occured allocating a bar type.");
    });

    return status(200).send( { "bar": bar } );
  })
  , get('/config/:resource', async ctx => {
    ctx.log.debug( "Processing request for " + ctx.params.resource );

    if( ctx.params.resource == "player" ) {
      return status(200).send( playerConfig );
    } else if ( ctx.params.resource == "levelCount" ) {
      if( levelConfigs != null ) return status(200).send(
        {
          "count": levelConfigs.length
        });
    } else {
      var levelNumber = parseInt( ctx.params.resource ) - 1;
      if( !isNaN(levelNumber)
          && levelNumber < levelConfigs.length
          && levelNumber >= 0
        ) return status(200).send( levelConfigs[ levelNumber ] );
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

    await final_model.count( { 'id': ctx.data.id } ).then( count => {
      if( count + 1 != ctx.data.trial )
        return status(403).send("Trial number mismatch");
    });

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

    await final_model.count( query ).then( count => {
      if( count == 0 ) entry_model.updateMany( query, update ).exec();
    }, err => { return status(500).send(err) } );
    return status(200);
  })
  , error( ctx => {
    ctx.log.error( ctx.error.message );
    return status(500).send(ctx.error.message);
  })
  , ctx => status(404)
]).then(ctx => {
  console.log(`Server launched on http://localhost:${ctx.options.port}/`)});

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