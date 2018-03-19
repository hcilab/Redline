const server = require('server');
const mongoose = require('mongoose');
const { get, post, put, error } = server.router;
const { render, json, status, header } = server.reply;

var redline_entry_schema =  mongoose.Schema({
    date: { type: Date, default: Date.now }
  , time: String
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
const entry_model = db.model( 'entry', redline_entry_schema );

let tableData = {
  entry: []
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
  },
  cors,
  [
    get( '/', ctx => render("index.hbs", tableData ) )
  , post('/', ctx => {
    ctx.log.info( ctx.data );
    tableData.entry.push( ctx.data );
    const entry = new entry_model( ctx.data );
    entry.save().then(()=> ctx.log.info('entry saved'));
    return status(200);
  })
  , error( ctx => {
    ctx.log.error( ctx.error.message );
    return status(500).send(ctx.error.message);
  })
]);
