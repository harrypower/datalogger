
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

: svg-attr#3 ( -- ) \ second set of attributes
    lineattrn-$off
    s" fill="           lineattrn-$!
    s" fill-opacity="   lineattrn-$!
    s" stroke="         lineattrn-$!
    s" stroke-opacity=" lineattrn-$!
    s" stroke-width="   lineattrn-$!
    lineattrv-$off
    s\" \"rgb(0,0,255)\""   lineattrv-$!
    s\" \"0.0\""            lineattrv-$!
    s\" \"rgb(0,100,200)\"" lineattrv-$!
    s\" \"1.0\""            lineattrv-$!
    s\" \"5.0\""            lineattrv-$!
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

: svgmakepath ( -- ) \ will start path tag with lineattr attrabutes
    \ svg-attr#1 \ still need to figure best way to pass these attributes *****
    s" <path " svgoutput$ $+!
    svgattrout svgoutput$ $+!
    s\" d=\" " svgoutput$ $+!
;
    
: svgpathdata ( -- ) \ use this directly after svgmakepath to put data into the path statement
    \ data comes from pathdat$ string array
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
    svg-attr#1
    svgmakepath
    svgpathdata
    svgend ;

variable circlejunk$
list$: circlesvg$
: makecirclefrompathdata ( -- )
    circlesvg$-$off
    svg-attr#2   \ will need to figure out best way for attributes to be passed here!  *****
    svgattrout 2drop
    pathdata$ swap drop 0 do
	s\" <circle cx=\"" circlejunk$ $!
	pathdata$-$@ 32 $split 2swap 2drop 32 $split 2swap circlejunk$ $+!
	s\" \" cy=\"" circlejunk$ $+! circlejunk$ $+!
	s\" \" r=\"4\"" circlejunk$ $+!
	attribute$ $@ circlejunk$ $+!
	s" />" circlejunk$ $+!
	circlejunk$ $@ circlesvg$-$!
    loop
    circlesvg$ swap drop 0 do
	circlesvg$-$@ svgoutput$ $+! 
    loop ;

list$: localdata

0 value mymin           \ will contain the chart data min absolute value
0 value mymax           \ will contain the chart data max absolute value
0 value xstep           \ how many x px absolute values to skip between data point plots
140 value xlablesize    \ the size taken up by the xlabel on the right side for chart
1300 value xmaxchart    \ the max x absolute px of the chart .. change this value to make chart larger in x
600 value ymaxchart     \ the max y absolute px of the chart .. change this value to make chart larger in y
140 value ylablesize    \ the y label at bottom of chart size in absolute px
70 value ytoplablesize  \ the y label at top of chart size in absolute px
0 value yscale          \ this is the scaling factor of the y values to be placed on chart
6 constant xminstep     \ the min distance in px between x ploted points 
xmaxchart xminstep /
constant xmaxpoints     \ this will be the max allowed points to be placed on the chart 
70 value ylableskippx   \ the px to skip to calculate how many y lables will be made
10 value xlableoffset   \ the offset to place lable from xlabelsize edge
10 value ylableoffset   \ the offset to place lable from ( ymaxchart + ytoplablesize )
variable working$
10 value ylableamounts  \ how many y lablelines and or text spots
bufr$: somejunk$

: findminmaxdata ( -- nmin nmax )
    0 0 { nmin nmax }
    localdata-$@ s>number? if d>s dup to nmin to nmax then
    localdata swap drop xmaxpoints min 0 do
	localdata-$@ s>number? if d>s dup nmin min to nmin nmax max to nmax then
    loop nmin nmax ;

: svgchartheader ( -- ) \ will produce the headerv string array stuff for the chart 
    \ make header size for svg
    \ note need to add the border top and the text bottom size also later
    headerv-$off
    s\" \"" working$ $!
    xmaxchart xlablesize + #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ headerv-$!
    s\" \"" working$ $!
    ymaxchart ylablesize + ytoplablesize + #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ headerv-$!
    s\" \"0 0 " working$ $!
    xmaxchart xlablesize + #to$ working$ $+!
    s"  " working$ $+!
    ymaxchart ylablesize + ytoplablesize + #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ headerv-$! ;

: svgchartmakepath ( -- ) \ will prduce the pathdata$ string array stuff for the chart
    \ recalculate data and form the path data statement for the ploted line
    pathdata$-$off
    localdata 2drop
    s" M " working$ $! xlablesize #to$  working$ $+! s"  " working$ $+!
    localdata-$@ s>number?
    if d>s mymax ymaxchart >
	if yscale / else yscale * then
	mymax yscale * swap - ytoplablesize + #to$
    else
	2drop s" 0"
    then
    working$ $+! working$ $@ pathdata$-$!
    localdata swap drop 1 localdata-$@ 2drop
    do
	s" L " working$ $!
	i xstep * xlablesize + #to$ working$ $+! s"  " working$ $+!
	localdata-$@ s>number?
	if d>s mymax ymaxchart >
	    if yscale / else yscale * then
	    mymax yscale * swap - ytoplablesize + #to$ working$ $+! working$ $@ pathdata$-$!
	else
	    drop
	then
    loop ;

: svgchartmakeylable ( -- )
    \ make the y lable line
    pathdata$-$off
    svg-attr#3  \ need to figure this attribute thing out!  *****
    svgmakepath
    s" M " working$ $! xlablesize xlableoffset - #to$ working$ $+! s"  " working$ $+!
    ytoplablesize #to$ working$ $+! s"  " working$ $+! working$ $@ pathdata$-$!
    s" L " working$ $! xlablesize xlableoffset - #to$ working$ $+! s"  " working$ $+!
    ymaxchart ytoplablesize + ylableoffset + #to$ working$ $+! s"  " working$ $+! working$ $@ pathdata$-$!
    \ make the x lable line
    s" L " working$ $! xmaxchart xlablesize + #to$ working$ $+! s"  " working$ $+!
    ymaxchart ytoplablesize + ylableoffset + #to$ working$ $+! s"  " working$ $+! working$ $@ pathdata$-$!
    svgpathdata
    
;

: makesvgchart ( ndata-index ndata-addr -- caddr u )
    localdata-$off
    localdata->$!  
    findminmaxdata to mymax to mymin
    xmaxchart localdata swap drop xmaxpoints min / to xstep
    ymaxchart mymax mymin - > 
    if
	ymaxchart mymax mymin - / to yscale
    else
	drop
	mymax mymin - ymaxchart / to yscale
    then
    svgchartheader
    svgchartmakepath
    svgmakehead
    svg-attr#1
    svgmakepath
    svgpathdata
    \ draw circle from the path data just made
    makecirclefrompathdata
    svgchartmakeylable
    svgend
;