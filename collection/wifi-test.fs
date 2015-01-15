#! /usr/local/bin/gforth
\ note this should call gforth version 0.7.2 and up  /usr/bin/gforth  would call 0.7.0 only!

\ This Gforth code simply checks wifi connection and resests wifi if not currently connected
\    Copyright (C) 2014  Philip K. Smith

\    This program is free software: you can redistribute it and/or modify
\    it under the terms of the GNU General Public License as published by
\    the Free Software Foundation, either version 3 of the License, or
\    (at your option) any later version.

\    This program is distributed in the hope that it will be useful,
\    but WITHOUT ANY WARRANTY; without even the implied warranty of
\    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\    GNU General Public License for more details.

\    You should have received a copy of the GNU General Public License
\    along with this program.  If not, see <http://www.gnu.org/licenses/>.


require script.fs

: ping-test ( -- nflag ) \ true flag means connection is up .. false flag returned means connection is down
    s\" (( ping -c1 google.ca > /dev/null 2>&1) && echo \"up\" || (echo \"down\" && exit 1)) "
    shget throw
    s" up" search swap drop swap drop ;

: test-connect ( -- ) \ test wifi connection and reconnect if not working
    ping-test false =
    if
	." Internet connection resetting now!" cr
	s" ./wifi-reset.sh" system
    else ." Internet connection ok!" cr
    then ;

: loop-test ( -- ) \ test wifi every 10 seconds
    begin 10000 ms test-connect again ;

loop-test
bye