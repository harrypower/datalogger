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

variable abuffer
1000 allocate throw abuffer ! \ store allocated heap address for buffer 
variable path-logging$


next-exception @ constant socket-errorListStart
s" Data from mbed incomplete!"                            exception constant mbedfail-err              
s" Socket failure in get-thp$ function"                   exception constant socketfail-err            
s" Mbed tcp socket package second terminator not present" exception constant mbedpackage2termfail-err  
s" Mbed tcp socket terminator not present"                exception constant mbedpackagetermfail-err   
s" Mbed tcp HTTP header missing"                          exception constant mbedhttpfail-err          
s" Mbed data message incomplete"                          exception constant mbedmessagefail-err       
s" Socket timeout failure in mbedread-client"             exception constant sockettime-err            
s" Socket message recieved to large"                      exception constant tcpoverflow-err          

next-exception @ constant socket-errorListEnd

struct
    cell% field http$
    cell% field socketterm$
    cell% field GET$
end-struct strings%

create mystrings strings% %allot drop

s" HTTP/1.0 200 OK"  mystrings http$ $!
s\" \r\n\r\n"        mystrings socketterm$ $!
s" GET /"            mystrings GET$ $!

list$: socketbuffer$s
list$: regdevice$s
list$: theconxtinfo$s

: findsockterm ( caddr u -- caddr1 u1 nflag ) \ nflag is true if socket terminator is found and caddr1 u1 is a
    \ new string past the terminator.  String is the same as caddr u but the first part is removed as well as
    \ terminator
    \ nflag is false if there was no terminator found and caddr1 and u1 will contain original string
    mystrings socketterm$ $@ search
    if
	mystrings socketterm$ $@ swap drop dup rot swap - rot rot + swap true
    else
	false
    then ;

: find2sockterm ( caddr u -- nflag ) \ looks for both socket terminators and returns true if it finds them
    findsockterm
    if
	findsockterm
	if
	    2drop true
	else
	    2drop false
	then
    else
	2drop false
    then ;

: mbedread-client ( caddr u nport# -- caddr1 u1 nflag )
    try
	open-socket { socketid }
	
	s\" GET /val \r\n\r\n" socketid write-socket
	abuffer 500 erase
	buffer2$ $off s" " buffer2$ $!
	utime
	begin
	    2dup
	    socketid abuffer 499 read-socket  \ note read-socket is for TCP read-socket-from is for UDP
	    buffer2$ $+!
	    utime 2swap d- d>s mbed-timeout# >
	    if
		socketid close-socket
		sockettime-err throw
	    then
	    buffer2$ $@ swap drop buff2max >
	    if
		socketid close-socket
		tcpoverflow-err throw
	    then
	    buffer2$ $@ find2sockterm
	until
	2drop
	socketid close-socket
	buffer2$ $@ false
    restore
	dup if swap drop then
    endtry ;

: get-thp$ ( -- caddr u nflag )  \ reads the mbed server and returns the data to be inserted into db
    try   \ flag is false for reading of mbed was ok any other value is some error
	mystrings mbed-ip$ $@ mbed-port# mbedread-client throw
	mystrings http$ $@ search
	if
	    2dup find2sockterm
	    if
		findsockterm drop
		4 - false
	    else
		2drop mbedpackage2termfail-err
	    then
	else
	    2drop mbedhttpfail-err
	then
    restore dup if 0 swap 0 swap then
    endtry ;

: get-sensor-data ( -- )
    registered-devices@
    regdevice$s-$off          \ empty string handler
    theconxtinfo$s-$off       \ empty connection info handler
    devices$ regdevice$s->$!  \ transfer device name list to regdevice$s
    connection$s theconxtinfo$s->$!  \ transfer connection info to theconxtinfo$s

    named-device-connection$
;


