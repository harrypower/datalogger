\ This Gforth code are miscellaneous tools

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

require string.fs
require ../Gforth-Tools/sqlite3_gforth_lib.fs

\ note pad will get clobered with some of the following words

variable path$
s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file path$ $!  \ configure sets this up to contain the full path


: error#to$ ( nerror -- caddr u )  \ takes an nerror number and gives the string for that error
    >r sqlmessg error-cell @ 0 <> r@ 1 >= r@ 101 <= and and r> swap 
    if
	dberrmsg drop \ test for a sqlite3 error and return sqlite3 string if the error is from sqlite3
    else
	>stderr Errlink   \ tested with gforth ver 0.7.0 and 0.7.3  
	begin             \ if the nerror does not exist then a null string is returned!
	    @ dup
	while
		2dup cell+ @ =
		if
		    2 cells + count rot drop exit
		then
	repeat
    then ;

: dto$ ( d -- caddr u )  \ convert double signed to a string
    swap over dabs <<# #s rot sign #> #>> ;

: #to$ ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    s>d
    swap over dabs
    <<# #s rot sign #> #>> ;

: #to$, ( n -- c-addr u1 ) \ convert n to string then add a "," at the end of the converted string
    #to$ pad $! s" ," pad $+! pad $@ ;

: datetime$ ( -- caddr u ) \ create the current time value of unixepoch and make into a string with a "," at the end of string
    utime 1000000 fm/mod swap drop
    #to$, ;

: init$ ( addr -- ) >r r@ off s" " r> $! ;  \ this is the same as $init in higher version of gforth.
\ use this to initalize a string variable before accessing the string in the variable

