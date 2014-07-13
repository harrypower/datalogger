#! /usr/local/bin/gforth
\ note this should call gforth version 0.7.2 and up  /usr/bin/gforth  would call 0.7.0 only!

\ This Gforth code is a Raspberry Pi Data logging code
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

\ This code is the socket interface type words for datalogging project
\ talk to and get communication back from a sensor

\ warnings off

require string.fs
require ../socket.fs  \ note this is the socket.fs included in this git rep
\ this socket.fs is a version for gforth but it is not compatable with version 0.7.0 that comes with apt-get install gforth
\ this socket.fs works in this code and the version 0.7.0 unix/socket.fs does not work with this code
require gforth-misc-tools.fs
require sqlite3-stuff.fs

decimal

0 value buffer
here to buffer 1000 allot
variable path-logging$


next-exception @ constant socket-errorListStart

next-exception @ constant socket-errorListEnd

: get-data ( -- )
    registered-devices@
    devices$-$@
    named-device-connection$
;

