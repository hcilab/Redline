const express = require('express');
const app = express();
const path = require('path');

app.use( express.static('public') );

app.use(function(req, res, next) {
  res.header("Access-Control-Allow-Origin", "*");
  res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
  next();
});

app.get('/', (req, res) => {
  res.header("Access-Control-Allow-Origin", "*");
  res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
  res.sendFile( path.join(__direname + 'public/index.html') );
});

app.listen(9500, () => console.log("Started server on port 9500"));
