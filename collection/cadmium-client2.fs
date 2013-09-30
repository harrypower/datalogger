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
include /home/pi/git/datalogger/collection/semaphore.fs

777 constant time-exceeded-fail
10000 constant fail-loop-limit
200 constant wait-time
12  constant response-time


66 constant running?
77 constant yes
88 constant get-cadmium-value
99 constant end-srv
55 constant time-error

0 value counter

sema% cadmium-ready  \ srv makes only
sema% cadmium-value  \ srv makes only
sema% cadmium-error  \ srv makes only
sema% cadmium-pin    \ client makes only
sema% cadmium-cmd    \ client makes only
sema% cadmium-pid
sema% cad-client-response


: hello-server ( -- nflag ) \ nflag is false if server has acknowledged. nflag is true if time out expired. 
    try
    0 to counter
	begin cadmium-ready sema-try- counter 1+ dup to counter swap if wait-time > if true throw else 1 ms false then else drop true then until
	false
    restore 
    endtry ;
    

: another-cad-srv-running? ( -- nflag ) \ nflag is true if there is another server running and responding!
    cadmium-ready sema-op-exist         \ nflag is false for any other possible issue other then a fully responding other server 
    if
	false s" No server running!" type cr
    else
	try
	    hello-server throw
	    running? cadmium-cmd sema-mk-named if cadmium-cmd sema-delete drop running? cadmium-cmd sema-mk-named throw then 
	    response-time ms
	    cadmium-error sema-op-exist throw \ if this throws then if server was there it did not respond in resonable time so assume server is not working
	    cadmium-error sema@ throw yes = if false s" Server error response!" type cr else true s" Server error response timeout!" type cr then 
	restore
	    if false s" Server not responding general timeout or error msg timeout!" type cr else true then 
	    cadmium-error sema-close drop
	    cadmium-cmd dup sema-close drop sema-delete if cadmium-cmd dup sema-close drop sema-delete throw then
	endtry
	cadmium-ready sema-close drop
    then ;

: end-cadmium-srv ( -- nflag )
    try
	cadmium-ready sema-op-exist throw
	hello-server throw
	end-srv cadmium-cmd sema-mk-named throw
	response-time ms
	cadmium-error sema-op-exist throw
	cadmium-error sema@ throw
	cadmium-error sema-close throw
	cadmium-cmd dup sema-close drop sema-delete throw
	cadmium-ready sema-close throw
    restore
	cadmium-error sema-close drop
	cadmium-cmd dup sema-close drop sema-delete drop
    endtry ;

: read-cadmium-cmd ( -- nvalue nflag )
    try
	cadmium-cmd sema-op-exist if cadmium-cmd dup sema-close drop sema-delete drop then 
	get-cadmium-value cadmium-cmd sema-mk-named throw  \ s" sent cmd!" type cr
	response-time ms
	cadmium-value sema-op-exist throw
	cadmium-error sema-op-exist throw 
	cadmium-value sema@ throw \ s" recieved value!" type cr
	cadmium-error sema@ throw \ s" recieved error!" type cr
	cadmium-value sema-close throw
	cadmium-error sema-close throw
	cadmium-cmd dup sema-close drop sema-delete throw \ s" closed cmd sema!" type cr
	false 
    restore dup if 0 swap else drop then
    endtry ;


: get-Cad-value ( -- nvalue nflag )
\    another-cad-srv-running? false =
\    if
\	s" sudo /home/pi/git/datalogger/collection/cadmium-srv2.fs &" system
\	wait-time ms
\    then
    cadmium-ready sema-op-exist throw
    hello-server throw
    read-cadmium-cmd
    cadmium-ready sema-close throw
;


