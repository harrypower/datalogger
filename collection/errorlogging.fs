\ This Gforth code is used with datalogger, Rpi_Gforth_GPIO software   
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

\ This gforth code is used as follows:
\ Include errorlogger.fs
\ This code does not run on its own and is used in other gforth code for easy error logging
\ The main word is called error_log and this word will simply store the date time information with an error number you pass it.
\ The location that the error log file is found below in the source code and could be changed to use for other projects.
\ Note there are several other words that can be used for other things of note filetest is the most useful as it allows testing if a file exists or not.

 
[ifundef] error_log
    
include ../string.fs
include script.fs


variable datetime$        \ will contain the current date and time string when datetime is called 
variable error_file$	  \ contains the path to the error.data file
variable junk$

junk$ $init
datetime$ $init
error_file$ $init
s" /home/pi/git/datalogger/collection/error.data" error_file$ $!

: filetest ( caddr u -- nflag )  \ looks for the full path and file name in string and returns true if found
    s" test -e " junk$ $! junk$ $+! s"  && echo 'yes' || echo 'no'" junk$ $+! junk$ $@ shget throw 1 -  s" yes" compare  
    if false
    else true
    then ;

: #to$ ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    s>d
    swap over dabs
    <<# #s rot sign #> #>>
    junk$ $! s" ," junk$ $+! junk$ $@ ;

: datetime ( -- )  \ string for data time created and placed in datetime$ variable
    time&date
    #to$ datetime$ $! #to$ datetime$ $+! #to$ datetime$ $+! #to$ datetime$ $+! #to$ datetime$ $+! #to$ datetime$ $+! ; 

: error_log ( nerror -- )  \ error_file$ contains file name of where nerror will be stored
    TRY
	error_file$ $@ filetest
	if error_file$ $@ w/o open-file throw 
	else error_file$ $@ w/o create-file throw 
	then { nfileid }
	nfileid file-size throw nfileid reposition-file throw
	#to$ nfileid write-file throw datetime datetime$ $@ nfileid write-line throw
	nfileid flush-file throw nfileid close-file throw false false 
    RESTORE drop drop
    ENDTRY ;

[then]
