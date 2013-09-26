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

variable junk$ junk$ $init
variable SEM_FAILED* semaphore-constants SEM_FAILED* ! value oflag
variable cad-ready* sem_failed* @ cad-ready* !
variable cad-ready$ cad-ready$ $init
variable cad-value* sem_failed* @ cad-value* !
variable cad-value$ cad-value$ $init
variable cad-error* sem_failed* @ cad-error* !
variable cad-error$ cad-error$ $init
variable cad-pin*   sem_failed* @ cad-pin* !
variable cad-pin$   cad-pin$ $init
variable cad-cmd*   sem_failed* @ cad-cmd* !
variable cad-cmd$   cad-cmd$ $init
variable sema-system-path$ sema-system-path$ $init

s" /dev/shm/sem." sema-system-path$ $!
s" cadmium-ready" cad-ready$ $!
s" cadmium-value" cad-value$ $!
s" cadmium-error" cad-error$ $!
s" cadmium-pin"   cad-pin$   $!
s" cadmium-cmd"   cad-cmd$   $!

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

;

: start-cad-srv ( -- nflag ) \ nflag is false if server was started ok and semaphore variables opened ok

;

: wait-for-cmd ( -- )

;

: process-cmd ( -- )

;

: do-cad-srv ( -- )
    start-cad-srv
    wait-for-cmd
    process-cmd
;

: process-cadmium ( -- )
    try
	another-cad-srv-running? throw
	do-cad-srv
    restore bye
    endtry ;

