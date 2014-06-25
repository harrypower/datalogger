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

\ note pad will get clobered with some of the following words

variable path$
s" /var/lib/datalogger-gforth/datalogger_home_path" slurp-file path$ $!  \ configure sets this up to contain the full path


: error#to$ ( nerror -- caddr u )  \ takes an nerror number and gives the string for that error
    >stderr Errlink   \ tested with gforth ver 0.7.0 and 0.7.3  
    begin             \ if the nerror does not exist then a null string is returned!
	@ dup
    while
	    2dup cell+ @ =
	    if
		2 cells + count rot drop exit
	    then
    repeat ;

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