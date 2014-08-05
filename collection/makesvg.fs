
\ This Gforth code is a Raspberry Pi Data logging code
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

\ This code has words to create svg code that can be used
\ directly inside html 

require gforth-misc-tools.fs
require string.fs

list$: lineattrn  \ svg attributes names to work with a line
list$: lineattrv  \ svg attribute values to work with a line but are paired with linattrn names

: svg-attr#1 ( -- ) \ initalizes lineattrn to default values
    lineattrn-$off
    s" fill="           lineattrn-$!
    s" fill-opacity="   lineattrn-$!
    s" stroke="         lineattrn-$!
    s" stroke-opacity=" lineattrn-$!
    s" stroke-width="   lineattrn-$!
    lineattrv-$off
    s\" \"rgb(255,0,0)\""   lineattrv-$!
    s\" \"0.0\""            lineattrv-$!
    s\" \"rgb(120,255,0)\"" lineattrv-$!
    s\" \"1.0\""            lineattrv-$!
    s\" \"2.0\""            lineattrv-$!
;
svg-attr#1

: svg-attr#2 ( -- ) \ second set of attributes
    lineattrn-$off
    s" fill="           lineattrn-$!
    s" fill-opacity="   lineattrn-$!
    s" stroke="         lineattrn-$!
    s" stroke-opacity=" lineattrn-$!
    s" stroke-width="   lineattrn-$!
    lineattrv-$off
    s\" \"rgb(255,0,0)\""   lineattrv-$!
    s\" \"1.0\""            lineattrv-$!
    s\" \"rgb(120,255,0)\"" lineattrv-$!
    s\" \"1.0\""            lineattrv-$!
    s\" \"3.0\""            lineattrv-$!
;    

list$: headern    \ header for svg .. normaly width and height 
list$: headerv    \ header values that are paired with headern names

: init-svg-header ( -- ) \ default svg header
    headern-$off
    s" width="       headern-$!
    s" height="      headern-$!
    s" viewBox="     headern-$!
    headerv-$off
    s\" \"100\""         headerv-$!
    s\" \"100\""         headerv-$!
    s\" \"0 0 100 100 \"" headerv-$!
;
init-svg-header

list$: pathdata$  \ the data values used in path... M,m,l,L and other path values 
s" M 0 30" pathdata$-$!
s" L 1 35" pathdata$-$!
s" L 2 40" pathdata$-$!
s" L 3 50" pathdata$-$!
s" L 4 20" pathdata$-$!

variable svgoutput$  \ the primary output of the assembled svg string output

: svgmakehead ( -- )  \ start with this word to make svg start tag
    svgoutput$ $off
    s" <svg " svgoutput$ $!
    headerv 2drop 
    headern swap drop 0 do
	headern-$@ svgoutput$ $+!
	headerv-$@ svgoutput$ $+!
	s"  " svgoutput$ $+!
    loop
    s\" >\n" svgoutput$ $+! ;

variable attribute$
: svgattrout ( -- caddr u )
    attribute$ init$
    lineattrv 2drop
    lineattrn swap drop 0 do
	lineattrn-$@ attribute$ $+!
	lineattrv-$@ attribute$ $+!
	s"  " attribute$ $+!
    loop attribute$ $@ ;

: svgmakepath ( -- ) \ will start path tag with lineattr.h1 attrabutes
    svg-attr#1
    s" <path " svgoutput$ $+!
    svgattrout svgoutput$ $+!
    s\" d=\" " svgoutput$ $+!
;
    
: svgpathdata ( -- ) \ use this directly after svgmakepath to put data into the path statement
    pathdata$ swap drop 0 do
	pathdata$-$@ svgoutput$ $+!
	s"  " svgoutput$ $+!
    loop
    s\" \"> </path>\n" svgoutput$ $+!
;

: svgend ( -- caddr u ) \ to finish the svg tag in the output string and deliver string
    s" </svg>" svgoutput$ $+!
    svgoutput$ $@ ;

: makesvg ( -- caddr u )  \ put all the parts together and output the final svg string
    svgmakehead
    svgmakepath
    svgpathdata
    svgend ;

variable circlejunk$
list$: circlesvg$
: makecirclefrompathdata ( -- )
    circlesvg$-$off
    svg-attr#2
    svgattrout 2drop
    pathdata$ swap drop 0 do
	s\" <circle cx=\"" circlejunk$ $!
	pathdata$-$@ 32 $split 2swap 2drop 32 $split 2swap circlejunk$ $+!
	s\" \" cy=\"" circlejunk$ $+! circlejunk$ $+!
	s\" \" r=\"4\"" circlejunk$ $+!
	attribute$ $@ circlejunk$ $+!
	s" />" circlejunk$ $+!
	circlejunk$ $@ circlesvg$-$!
    loop ;

list$: localdata
: findminmaxdata ( -- nmin nmax )
    1000000 -1000000 { nmin nmax } 
    localdata swap drop 0 do
	localdata-$@ s>number? if d>s dup nmin min to nmin nmax max to nmax then
    loop nmin nmax ;

0 value mymin
0 value mymax
0 value xstep
500 constant xmaxchart
variable working$

: makesvg2 ( ndata-index ndata-addr -- caddr u )
    localdata->$!  
    findminmaxdata to mymax to mymin
    xmaxchart localdata swap drop / to xstep
    \ make header size for svg
    \ note need to add the border top and the text bottom size also later
    headerv-$off
    s\" \"" working$ $!
    xmaxchart #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ headerv-$!
    s\" \"" working$ $!
    mymax mymin - #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ headerv-$!
    s\" \"0 0 " working$ $!
    xmaxchart #to$ working$ $+!
    s"  " working$ $+!
    mymax mymin - #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ headerv-$!
    \ recalculate data and form the path data statement for the ploted line
    pathdata$-$off
    localdata 2drop 
    s" M 0 " working$ $!
    localdata-$@ s>number? if d>s mymax swap - #to$ else 2drop s" 0" then
    working$ $+! working$ $@ pathdata$-$!
    localdata swap drop 1 localdata-$@ 2drop 
    do
	s" L " working$ $!
	i xstep * #to$ working$ $+! s"  " working$ $+!
	localdata-$@ s>number? if d>s mymax swap - #to$ working$ $+! working$ $@ pathdata$-$! else drop then 
    loop
    svgmakehead
    svgmakepath
    svgpathdata
    \ draw circle from the path data just made
    makecirclefrompathdata
    circlesvg$ swap drop 0 do
	circlesvg$-$@ svgoutput$ $+! 
    loop
    svgend
;