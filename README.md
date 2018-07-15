# Redline
A firefighting game to study the relationship between information visualization and player behaviour.

## Running the system

Check that port 80 is forwarded to port 9501 as described below.

We are using `pm2` to run the server. The configuration file for both BOFS and the RedlineDataServer can be found in `home/jwuertz/ecosystem.json`.

You can see the state of the servers vai `pm2 list` and restart them both via `pm2 restart all`.

If you change the `ecosystem.json` file (i.e. you wish to add environment variables) you must run `pm2 reload ecosystem.json` from the directory.

Speaking of environment variables: You can add any environment variable that your server might want by adding it into the `env` section of the app in the `ecosystem.json` file.

For example the default port is set like so:

```
"env": {
  "PORT": 9500
},
```

## IMPORTANT

In order for this to work the game has to run on port 80 of the `hcidev.cs.unb.ca` server. Or an additional rule has to be added to the CORS configuration of the Nodejs server.


~~Currently this is achieved with a port forwarding rule from `80 => 9501` which forwards requests to the BOFS server that is serving the game.~~
~~This rule can be created via~~

~~`sudo iptables -t nat -A PREROUTING -i eth0 -p tcp --dport 80 -j REDIRECT --to-port 9501`~~

~~You can make sure the rule is in place by running:~~

~~`sudo iptables -t nat -L -n -v`~~

This is achieved via nginx now. The configuration file for this project can be found in `/etc/nginx/sites-enabled`.

The current BOFS server can be found at `/var/www/bofs/redline`.

## Structure

### `/Redline`

Contains Unity sources for the game.

### `/RedlineDataServer`

Contains sources for the Nodejs data server as well as the WebGL build files in `/RedlineDataServer/public`.
