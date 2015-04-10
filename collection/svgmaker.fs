\ This Gforth code is BeagleBone Black Gforth SVG maker 
\    Copyright (C) 2015  Philip K. Smith

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
    cell% inst-var svgmaker-test \ a construct test value to prevent memory leaks
  protected
    m: ( caddr u svgmaker -- ) \ append string into svg-output string
	svg-output @ [bind] string !+$ ;m method !svg$

    m: ( nstrings svgmaker -- ) \ place contents of nstrings into svg string as attribute propertys
	dup $qty 0 ?do
	    dup [bind] strings @$x this [current] !svg$ s"  " this [current] !svg$
	loop drop ;m method svgattr

    m: ( n svgmaker -- ) \ convert n to string and send string to !svg$ method
	s>d swap over dabs <<# #s rot sign #> #>>
	this [current] !svg$ ;m method #tosvg$
    
  public
    m: ( svgmaker -- ) \ init svg-output string
	svgmaker-test svgmaker-test @ =
	if \ svg-output should alreadys have a string so remove then recreate
	    svg-output @ [bind] string string-destruct
	    svg-output @ free throw
	    string heap-new svg-output !
	else \ svg-output not setup so just create string object
	    string heap-new svg-output ! svgmaker-test svgmaker-test !
	then  ;m overrides construct

    m: ( svgmaker -- ) \ free memory for this object and delete object
	svgmaker-test svgmaker-test @ =
	if
	    svg-output @ [bind] string string-destruct
	    svg-output @ free throw
	    0 svgmaker-test !
	then ;m method svgmaker-destruct
    
    m: ( nstrings-header svgmaker -- ) \ start svg string and place nstrings contents as header to svg
	s" <svg " svg-output @ [bind] string !$
	this svgattr
	s\" >" this [current] !svg$ ;m method svgheader

    m: ( nstrings-attr nx ny nstring-text svgmaker -- ) \ to make svg text 
	\ nstirngs-attr is strings for attribute of text
	\ nx ny are text x and y of svg text tag
	\ nstring-text is the string object address of the string
	s\" <text x=\"" this [current] !svg$ rot
	this [current] #tosvg$
	s\" \" y=\"" this [current] !svg$
	swap this [current] #tosvg$
	s\" \" " this [current] !svg$
	swap this [current] svgattr s" >" this [current] !svg$
        @$ this [current] !svg$ s" </text>" this [current] !svg$ ;m method svgtext

    m: ( nstrings-attr nstrings-pathdata svgmaker -- ) \ make a svg path with nstring-attr and nstrings-pathdata
	s" <path " this [current] !svg$
	s\" d=\"" this [current] !svg$
	this [current] svgattr s\" \" " this [current] !svg$
	this [current] svgattr s\" \/>" this [current] !svg$
	\ s\" \" \/>" this !svg$
    ;m method svgpath

    m: ( nstring-attr nx ny nr svgmaker -- ) \ make a svg circle with nstring-attr at nx and ny with radius nr
	s\" <circle cx=\"" this [current] !svg$ rot this [current] #tosvg$
	s\" \" cy=\"" this [current] !svg$ swap this [current] #tosvg$
	s\" \" r=\"" this [current] !svg$ this [current] #tosvg$
	s\" \" " this [current] !svg$ this [current] svgattr
	s" />" this [current] !svg$ ;m method svgcircle

    m: ( svgmaker -- caddr u ) \ finish forming the svg string and output it
	s" </svg>" svg-output @ [bind] string !+$
	svg-output @ [bind] string @$ ;m method svgend
    
    m: ( svgmaker -- caddr u ) \ view string directly
	svg-output @ [bind] string @$ ;m overrides print
end-class svgmaker


\ strings heap-new constant head1
\ strings heap-new constant attr1
\ strings heap-new constant path1
\ string heap-new constant a$
\ svgmaker heap-new constant thesvg

\ s\" width=\"300\""            head1 !$x
\ s\" height=\"300\""           head1 !$x
\ s\" viewBox=\"0 0 300 300 \"" head1 !$x

\ s\" fill=\"rgb(0,0,255)\""      attr1 !$x 
\ s\" fill-opacity=\"1.0\""       attr1 !$x 
\ s\" stroke=\"rgb(0,100,200)\""  attr1 !$x 
\ s\" stroke-opacity=\"0.0\""     attr1 !$x 
\ s\" stroke-width=\"4.0\""       attr1 !$x 
\ s\" font-size=\"20px\""         attr1 !$x 

\ s" M 10 30" path1 !$x 
\ s" L 15 35" path1 !$x 
\ s" L 27 40" path1 !$x 
\ s" L 48 50" path1 !$x 
\ s" L 97 20" path1 !$x 

\ s" Some test text!" a$ !$

\ head1 thesvg svgheader
\ attr1 30 20 a$ thesvg svgtext
\ thesvg svgend dump
\ thesvg print cr type cr

\ thesvg construct

\ head1 thesvg svgheader
\ attr1 path1 thesvg svgpath
\ thesvg svgend dump
\ thesvg print cr type cr

\ thesvg construct

\ head1 thesvg svgheader
\ attr1 50 80 35 thesvg svgcircle
\ thesvg svgend dump
\ thesvg print cr type cr
