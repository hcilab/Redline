# Redline
A firefighting game to study the relationship between information visualization and player behaviour.

## IMPORTANT

In order for this to work the game has to run on port 80 of the `hcidev.cs.unb.ca` server. Or an additional rule has to be added to the CORS configuration of the Nodejs server.

Currently this is achieved with a port forwarding rule from `80 => 9501` which forwards requests to the BOFS server that is serving the game.
This rule can be created via

`sudo iptables -t nat -A PREROUTING -i eth0 -p tcp --dport 80 -j REDIRECT --to-port 9501`

The current BOFS server can be found at `/var/www/bofs/redline`.

## Structure

### `/Redline`

Contains Unity sources for the game.

### `/RedlineDataServer`

Contains sources for the Nodejs data server as well as the WebGL build files in `/RedlineDataServer/public`.
