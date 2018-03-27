const server = require('server');
const mongoose = require('mongoose');
const { get, post, put, error } = server.router;
const { render, json, status, header } = server.reply;

var redline_entry_schema =  mongoose.Schema({
    date: { type: Date, default: Date.now }
  , time: String
  , counter: String
  , id: Number
  , level: String
  , hp: Number
  , bar_type: String
  , damage: Number
  , score: Number
  , proximity: Number
  , active: Number
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

const cors = [
  ctx => header("Access-Control-Allow-Origin", "*"),
  ctx => header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept"),
  ctx => ctx.method.toLowerCase() === 'options' ? 200 : false
];

server(
  {
    security: {
      csrf: false
    }
    , port: 9500
  },
  cors,
  [
    get( '/', ctx => render("index.hbs", tableData ) )
  , post('/', async ctx => {
    ctx.log.debug( ctx.data );
    tableData.atomic_entries.push( ctx.data );
    const entry = new entry_model( ctx.data );
    await entry.save();
    ctx.log.info('creating atomic entry for session ' + ctx.data.id );
    return status(200);
  })
  , post('/final/', async ctx => {
    ctx.log.debug( ctx.data );
    tableData.cumulative_entries.push( ctx.data );
    const entry = new final_model( ctx.data );
    await entry.save();
    ctx.log.info('creating final entry for session ' + ctx.data.id );
    return status(200);
  })
  , error( ctx => {
    ctx.log.error( ctx.error.message );
    return status(500).send(ctx.error.message);
  })
]);
