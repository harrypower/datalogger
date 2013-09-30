#! /usr/local/bin/gforth

\ This Gforth code will read a Cadmium sensor on a GPIO pin with just a capacitor and a resistor. 
\    Copyright (C) 2013  Philip K. Smith

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
\
\    This code works with version 2 boards but could work with other boards

warnings off

include /home/pi/git/datalogger/gpio/rpi_GPIO_lib.fs
include /home/pi/git/datalogger/string.fs
include /home/pi/git/datalogger/collection/semaphore.fs

777 constant time-exceeded-fail
10000 constant fail-loop-limit
200 constant wait-time
5  constant response-time

66 constant running?
77 constant yes
88 constant get-cadmium-value
99 constant end-srv
55 constant time-error

0 value counter
variable junk$ junk$ $init
variable sema-system-path$ sema-system-path$ $init

s" /dev/shm/sem." sema-system-path$ $!

sema% cadmium-ready  \ srv makes only
sema% cadmium-value  \ srv makes only
sema% cadmium-error  \ srv makes only
sema% cadmium-pin    \ client makes only
sema% cadmium-cmd    \ client makes only
sema% cadmium-pid
sema% cad-client-response

: shget ( caddr u -- caddr1 u1 nflag )  \ nflag is false if caddr1 and u1 are valid messages from system 
    try	
	r/o open-pipe throw dup >r slurp-fid
	r> close-pipe throw to $?
	false
    restore
    endtry ;

: filetest ( caddr u -- nflag ) \ nflag is false if the file is present
    try junk$ $init
	s" test -e " junk$ $! junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw
	s" no" search  
    restore swap drop swap drop  
    endtry ;

: CdS-raw-read { ncadpin -- ntime nflag } \ nflag is 0 if ntime is a real value
    try 
	piosetup throw
	ncadpin pipinsetpulldisable throw
	ncadpin pipinoutput throw
	ncadpin pipinlow throw
	4 ms
	ncadpin pipininput throw
	utime
	fail-loop-limit
	begin
	    1 - dup 
	    if
		ncadpin pad pipinread throw
		pad c@
	    else
		drop time-exceeded-fail throw
	    then
	until
	utime rot drop 2swap d- d>s
	piocleanup throw
	false
   restore dup if dup -9 = if 0 swap else 0 swap piocleanup drop then  then
   endtry ;


: another-cad-srv-running? ( -- nflag ) \ nflag is true if there is another server running and responding!
    cadmium-ready sema-op-exist         \ nflag is false for any other possible issue other then a fully responding other server 
    if
	false s" No other server running!" type cr
    else
	try
	    0 to counter
	    begin cadmium-ready sema-try- counter 1+ dup to counter swap if wait-time > if true throw else 1 ms false then else drop true then until 
	    running? cadmium-cmd sema-mk-named if cadmium-cmd sema-delete drop running? cadmium-cmd sema-mk-named throw then 
	    response-time ms
	    cadmium-error sema-op-exist throw \ if this throws then if server was there it did not respond in resonable time so assume server is not working
	    cadmium-error sema@ throw yes = if false s" Server responding!" type cr else true s" Other detected server not responding!" type cr then 
	restore
	    if false s" Other detected server not responding at error sema!" type cr else true then 
	    cadmium-error sema-close drop
	    cadmium-cmd dup sema-close drop sema-delete if cadmium-cmd dup sema-close drop sema-delete drop then
	endtry
	cadmium-ready sema-close drop
    then ;

: start-cad-srv ( -- ) \ this will start the server by making cadmium-ready semaphore at 0
    \ this word will throw if for some reason the cadmium-ready semaphore could not be started!
    0 cadmium-ready sema-mk-named
    if cadmium-ready dup sema-close drop sema-delete drop 0 cadmium-ready sema-mk-named if cadmium-ready dup sema-close drop sema-delete drop true throw then then ;

: wait-for-cmd ( -- ) \ this manages ready signal to clients to get ready for the commands from clients
    \ this word will throw if for some reason the cadmium-ready semaphore does not work correctly 
    cadmium-ready sema+
    if cadmium-ready dup sema-close drop sema-delete drop start-cad-srv cadmium-ready sema+ throw then
    begin
	1 ms
	cadmium-ready sema@ if drop cadmium-ready sema@ throw then
	0 =
    until ;

: process-running-cmd ( -- ) \ creates cadmium-error sema and puts yes into it.  If this cant be done it throws so this server can be shutdown and asking client can restart server
    yes cadmium-error sema-mk-named if cadmium-error dup sema-close drop sema-delete drop yes cadmium-error sema-mk-named if cadmium-error dup sema-close drop sema-delete drop true throw then then
    wait-time ms
    cadmium-error dup sema-close drop sema-delete drop ;

: process-get-cadmium-value ( -- ) \ need to add the pin value to this 
    7 cds-raw-read { nvalue nerror } nvalue . s"   " type nerror . cr
    nvalue cadmium-value sema-mk-named if 690 cadmium-value sema-mk-named if cadmium-value dup sema-close drop sema-delete drop true throw then then
    nerror cadmium-error sema-mk-named if 0 cadmium-error sema-mk-named if cadmium-error dup sema-close drop sema-delete drop true throw then then
    wait-time ms
    cadmium-error dup sema-close drop sema-delete if cadmium-error dup sema-close drop sema-delete throw then
    cadmium-value dup sema-close drop sema-delete if cadmium-value dup sema-close drop sema-delete throw then ;

: process-time-error ( -- )
    time-error cadmium-error sema-mk-named if cadmium-error dup sema-close drop sema-delete drop time-error cadmium-error sema-mk-named if cadmium-error dup sema-close drop sema-delete drop true throw then then
    wait-time ms
    cadmium-error dup sema-close drop sema-delete if cadmium-error dup sema-close drop sema-delete throw then ;

: process-end-srv ( -- )
    0 cadmium-error sema-mk-named if 0 cadmium-error sema-mk-named if cadmium-error dup sema-close drop sema-delete drop true throw then then
    wait-time ms
    cadmium-ready dup sema-close drop sema-delete drop
    cadmium-value dup sema-close drop sema-delete drop
    cadmium-error dup sema-close drop sema-delete drop ;

: process-cmd ( -- )
    0 to counter
    begin cadmium-cmd sema-op-exist if counter 1+ dup to counter wait-time > if 0 true else 1 ms false then else cadmium-cmd sema@ false = then until
    dup running? =          if process-running-cmd s" Asked if running!" type cr then
    dup get-cadmium-value = if process-get-cadmium-value s" Value delivered!" type cr then 
    dup end-srv =           if process-end-srv s" Asked to close this server process. Closing now!" type cr true throw then
        0 =                 if process-time-error s" Client exceeded command delivery time!" type cr then
    cadmium-cmd sema-close if cadmium-cmd sema-close drop then	
    ;

: do-cad-srv ( -- )
    start-cad-srv
    begin
	wait-for-cmd
	process-cmd
    again ;

: process-cadmium ( -- )
    try
	another-cad-srv-running? dup if s" Another server is currently running closing this process now!" type cr else s" Started this process as server!" type cr then  throw
	do-cad-srv
    restore
	bye
    endtry ;

process-cadmium