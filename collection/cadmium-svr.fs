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


include /home/pi/git/datalogger/gpio/rpi_GPIO_lib.fs
include /home/pi/git/datalogger/string.fs

777 constant time-exceeded-fail

variable cmd-fifo$
variable value-fifo$
variable fifo-path$ 
variable junk$

s" /var/lib/datalogger-gforth/" fifo-path$ $!
s" cadmium_cmd" cmd-fifo$ $!
s" cadmium_value" value-fifo$ $!

: shget ( caddr u -- caddr1 u1 nflag )  \ nflag is false if caddr1 and u1 are valid messages from system 
    try	
	r/o open-pipe throw dup >r slurp-fid
	r> close-pipe throw to $?
	false
    restore
    endtry    
;

: filetest ( caddr u -- nflag ) \ nflag is false if the file is present
    try junk$ $init
	s" test -e " junk$ $! junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw
	s" no" search  
    restore swap drop swap drop  
    endtry ;

: cmd-path$ ( -- caddr u )
    junk$ $init
    fifo-path$ $@ junk$ $! cmd-fifo$ $@ junk$ $+! junk$ $@ ;

: value-path$ ( -- caddr u )
    junk$ $init
    fifo-path$ $@ junk$ $! value-fifo$ $@ junk$ $+! junk$ $@ ;

: setup-cadmium ( -- )
    fifo-path$ $@ filetest
    if junk$ $init s" sudo mkdir " junk$ $! fifo-path$ $@ junk$ $+! junk$ $@ system $? throw then

    cmd-path$ filetest
    if cmd-path$ junk$ $init s" sudo mkfifo -m 777 " junk$ $! junk$ $+! junk$ $@ system $? throw then

    value-path$ filetest
    if value-path$ junk$ $init s" sudo mkfifo -m 777 " junk$ $! junk$ $+! junk$ $@ system $? throw then
;

: close-cadmium-svr ( -- )
    cmd-path$ filetest false =
    if cmd-path$ junk$ $init s" sudo rm " junk$ $! junk$ $+! junk$ $@ system $? throw then

    value-path$ filetest false =
    if value-path$ junk$ $init s" sudo rm " junk$ $! junk$ $+! junk$ $@ system $? throw then
    
;


: CdS-raw-read { ncadpin -- ntime nflag } \ nflag is 0 if ntime is a real value
    try 
	piosetup throw
	ncadpin pipinsetpulldisable throw
	ncadpin pipinoutput throw
	ncadpin pipinlow throw
	4 ms
	ncadpin pipininput throw
	utime
	1000
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
   endtry
    ;

: cad-value-write ( -- nflag ) \ nflag is false for sensor read and data put into fifo and file io all ok
    try
	7 CdS-raw-read throw
	value-path$ w/o open-file throw
	{ value-id }
	s>d swap over dabs
	<<# #s rot sign #> #>>
	s" cv=" pad swap move
	pad 3 + swap 3 + dup >r move pad r>
	value-id write-file throw
	value-id close-file throw 
	false 
    restore   
    endtry
;

: sensor-msg ( -- nflag ) \ nflag is false for message recieved and action was taken 
    try
	cmd-path$ r/o open-file throw 0 0 
	{ cmd-id caddr u }
	cmd-id slurp-fid to u to caddr
	cmd-id close-file throw
	caddr u s" read" search
	if 2drop cad-value-write throw
	else 2drop then
	caddr u s" end" search
	if 2drop true throw then 
	false 
    restore 
    endtry
;

: process-cadmium-sensor
    
    try	
	begin sensor-msg throw again
    restore 
    endtry ;

setup-cadmium process-cadmium-sensor close-cadmium-svr bye 



