const server = require('server');
const mongoose = require('mongoose');
const { get, post, put, error } = server.router;
const { render, json, status, header } = server.reply;

var redline_entry_schema =  mongoose.Schema({
    date: { type: Date, default: Date.now }
  , time: String
  , counter: String
  , id: { type: Number, unique: true }
  , level: String
  , hp: Number
  , bar: String
  , damage: Number
  , score: Number
  , proximity: Number
  , avg_intensity_in_proximity: Number
  , active: Number
});

var redline_final_entry_schema =  mongoose.Schema({
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
});

var uri = "mongodb://admin:URXEBCt5jyU6@cluster0-shard-00-00-y246y.mongodb.net:27017,cluster0-shard-00-01-y246y.mongodb.net:27017,cluster0-shard-00-02-y246y.mongodb.net:27017/redline?ssl=true&replicaSet=Cluster0-shard-0&authSource=admin";
mongoose.connect(uri);

// mongoose.connect("mongodb://localhost/redline");
var db = mongoose.connection;
const entry_model = db.model( 'atomic_entries', redline_entry_schema );
const final_model = db.model( 'cumulative_entries', redline_final_entry_schema );

let tableData = {
  atomic_entries: [],
  cumulative_entries: []

};

const corsExpress = require('cors')({
  origin: ['http://hcidev.cs.unb.ca', 'https://hcidev.cs.unb.ca']
});

const cors = server.utils.modern(corsExpress);

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
  , post('/', async ctx => {
    ctx.log.debug( ctx.data );
    tableData.atomic_entries.push( ctx.data );
    const entry = new entry_model( ctx.data );
    await entry.save();
    ctx.log.info('ATOMIC ENTRY ' + ctx.data.id + ' ' + ctx.data.level );
    ctx.log.debug(ctx.data);
    return status(200).send("data successfully logged");
  })
  , post('/final/', async ctx => {
    ctx.log.debug( ctx.data );
    tableData.cumulative_entries.push( ctx.data );
    const entry = new final_model( ctx.data );
    await final_model.count( { 'id': entry.id } ).then( count => {
      entry.trial = count;
    });
    await entry.save();
    ctx.log.info(
      'FINAL ENTRY ' + ctx.data.id
    + ' TRIAL ' + entry.trial
    + ' BAR ' + ctx.data.bar
    + ' ' + ctx.data.level );
    ctx.log.debug( ctx.data );
    return status(200).send("data successfully logged");
  })
  , error( ctx => {
    ctx.log.error( ctx.error.message );
    return status(500).send(ctx.error.message);
  })
  , ctx => status(404)
]);

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
