\ This Gforth code is BeagleBone Black Gforth SVG maker 
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

\ The code makes an object using object.fs to create and work with SVG 
\ output for using in a web server for example!

require stringobj.fs  \ this is my own string package for single strings and array of strings

object class
    cell% inst-var svg-output \ the svg output string object
    cell% inst-var construct-test \ a construct test value to prevent memory leaks
  protected
  public
    m: ( svg -- ) \ init svt-output string
	construct-test construct-test @ =
	if svg-output @ construct \ init string
	else string heap-new svg-output ! construct-test construct-test !
	then  ;m overrides construct
    
    m: ( nstrings svg -- ) \ start svg string and place nstrings contents as header to svg
	s" <svg " svg-output @ !$
	dup len$ 0 ?do
	    dup @$x svg-output @ !+$ s"  " svg-output @ !+$
	loop drop
	s\" >\n" svg-output @ !+$ ;m method svgheader

    m: ( nstrings svg -- ) \ place contents of nstrings into svg string as attribute propertys
    ;m method svgattr

    m: ( nstrings-attr nx ny nstring-text svg -- ) \ to make svg text 
	\ nstirngs-attr is strings for attribute of text
	\ nx ny are text x and y of svg text tag
	\ nstring-text is the string object address of the string
    ;m method svgtext

    m: ( nstrings-attr nstrings-pathdata svg -- ) \ make a svg path with nstring-attr and nstrings-pathdata
    ;m method svgpath

    m: ( nstring-attr nx ny nr -- ) \ make a svg circle with nstring-attr at nx and ny with radius nr
    ;m method svgcircle

    m: ( -- caddr u ) \ finish forming the svg string and output it
	s" </svg>" svg-output @ !+$
	svg-output @ @$ ;m method svgend
    
    m: ( -- caddr u ) \ view string directly
	svg-output @ @$ ;m overrides print
end-class svgmaker

strings heap-new constant test$s
svgmaker heap-new constant test