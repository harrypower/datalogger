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

variable cmd-fifo$ cmd-fifo$ $init
variable value-fifo$ value-fifo$ $init
variable fifo-path$ fifo-path$ $init
variable junk$ junk$ $init
variable mysem_t*
variable sema-name$ sema-name$ $init
variable sema-system-path$ sema-system-path$ $init

s" /dev/shm/sem." sema-system-path$ $!
s" cadmium-available" sema-name$ $!
s" /var/lib/datalogger-gforth/" fifo-path$ $!
s" cadmium_cmd" cmd-fifo$ $!
s" cadmium_value" value-fifo$ $!


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

: cmd-path$ ( -- caddr u )
    junk$ $init
    fifo-path$ $@ junk$ $! cmd-fifo$ $@ junk$ $+! junk$ $@ ;

: value-path$ ( -- caddr u )
    junk$ $init
    fifo-path$ $@ junk$ $! value-fifo$ $@ junk$ $+! junk$ $@ ;

: sema-path$ ( -- caddr u )
    junk$ $init
    sema-system-path$ $@ junk$ $! sema-name$ $@ junk$ $+! junk$ $@ ;

: existing-sema ( -- )
    sema-name$ $@ semaphore-op-existing throw
    mysem_t* ! ;

: make-sema ( -- )
    sema-name$ $@ 0 semaphore-mk-named
    if
       existing-sema	
    else
	mysem_t* !
    then ;

: close-sema ( -- )
    mysem_t* @ semaphore-constants swap drop <> 
    if  mysem_t* @ semaphore-close throw
	sema-name$ $@ semaphore-delete throw
    then ;

: make-fifo ( -- )
    fifo-path$ $@ filetest
    if junk$ $init s" sudo mkdir " junk$ $! fifo-path$ $@ junk$ $+! junk$ $@ system $? throw then

    cmd-path$ filetest
    if cmd-path$ junk$ $init s" sudo mkfifo -m 664 " junk$ $! junk$ $+! junk$ $@ system $? throw then

    value-path$ filetest
    if value-path$ junk$ $init s" sudo mkfifo -m 664 " junk$ $! junk$ $+! junk$ $@ system $? throw then ;

: close-fifo ( -- )
    cmd-path$ filetest false =
    if cmd-path$ junk$ $init s" sudo rm " junk$ $! junk$ $+! junk$ $@ system $? throw then

    value-path$ filetest false =
    if value-path$ junk$ $init s" sudo rm " junk$ $! junk$ $+! junk$ $@ system $? throw then ;

: cadmium-srv-running? ( -- nflag ) \ nflag is false if there is no other cadmium srv software running
    \ nflag is true if server is running and  responding
    try
	sema-path$ filetest              
	if                               
	    \ sema not present so server not running so start this process as server
	    false
	else
	    \ sema present so try talking to it to see if it is working
	    existing-sema
	    utime
	    begin
		2dup utime 2swap d- d>s 1000000 <
		if
		    mysem_t* @ semaphore-try- false =
		else
		    true
		then
	    until
	    utime 2swap d- d>s 1000000 <
	    if  \ semaphore was decremented now ask if running?
		cmd-path$ w/o open-file throw dup 
		s" running?" rot write-file throw
		close-file throw
		value-path$ r/o open-file throw
		dup pad 20 rot read-file throw
		swap close-file throw
		pad swap s" yes!" search
		if 2drop true \ server responded to running ? with yes! so it is running 
		else 2drop false \ server did not respond it is not running
		then 
	    else  \ timed out so server must not be working!
		mysem_t* semaphore-close throw
		sema-name$ $@ semaphore-delete throw
		close-fifo
		false
	    then
	then
    restore 
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

: #to$ ( n -- caddr u ) \ convert signed number to string
    junk$ $init
    s>d swap over dabs <<# #s rot sign #> junk$ $! junk$ $@ #>> ;

: cad-value-write ( npin -- nflag ) \ nflag is false for sensor read and data put into fifo and file io all ok
    try
	CdS-raw-read throw
	value-path$ w/o open-file throw
	{ value-id }
	#to$
	s" cv= " pad swap move
	pad 4 + swap 4 + dup >r move pad r>
	value-id write-file throw
	value-id close-file throw 
	false 
    restore 
    endtry ;

: running-reply ( -- nflag ) \ nflag is false for running reply yes sent
    try 
	value-path$ w/o open-file throw
	s" yes!" rot dup >r write-file throw
	r> close-file throw
	false
    restore
    endtry ;

: getpin ( caddr u -- npin ) \ npin will be zero for no pin or a GPIO pin number that cadmium sensor is attached to
   s>unumber? if d>s else 2drop 0 then ;

: sensor-msg ( -- nflag ) \ nflag is false for message recieved and action was taken 
    try
	cmd-path$ r/o open-file throw 0 0 
	{ cmd-id caddr u }
	cmd-id slurp-fid to u to caddr
	cmd-id close-file throw
	caddr u s" read" search
	if 2drop 7 cad-value-write throw
	\ if 6 - swap 5 + swap getpin cad-value-write throw
	else 2drop then
	caddr u s" end" search
	if 2drop true throw then
	caddr u s" running?" search
	if 2drop running-reply throw then
	false 
    restore 
    endtry ;

: say-ready ( -- nflag ) \ nflag is false if server has told the world it is ready 
    mysem_t* @ semaphore+ ;

: message-ready ( -- )
    begin
	mysem_t* @ semaphore@ throw 0 =
	if true else 2 ms false then
    until ;

: process-cadmium-sensor ( -- )
\    cadmium-srv-running? false =
\    if
	try
	    make-sema
	    make-fifo
	    begin
		say-ready throw
		message-ready
		sensor-msg throw
	    again
	restore
	endtry
	close-sema
	close-fifo
\    then
    bye ;

process-cadmium-sensor   



