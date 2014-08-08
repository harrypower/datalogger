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

: svg-attrtext ( -- ) \ basic text attributes
    lineattrn-$off
    s" fill="           lineattrn-$!
    s" fill-opacity="   lineattrn-$!
    s" stroke="         lineattrn-$!
    s" stroke-opacity=" lineattrn-$!
    s" stroke-width="   lineattrn-$!
    s" font-size="      lineattrn-$!
    lineattrv-$off
    s\" \"rgb(0,0,255)\""   lineattrv-$!
    s\" \"1.0\""            lineattrv-$!
    s\" \"rgb(0,100,200)\"" lineattrv-$!
    s\" \"0.0\""            lineattrv-$!
    s\" \"2.0\""            lineattrv-$!
    s\" \"20px\""           lineattrv-$!
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
: init-test-path ( -- )
    pathdata$-$off
    s" M 10 30" pathdata$-$!
    s" L 15 35" pathdata$-$!
    s" L 27 40" pathdata$-$!
    s" L 48 50" pathdata$-$!
    s" L 97 20" pathdata$-$! ;

variable svgoutput$  \ the primary output of the assembled svg string output

: svgmakehead ( -- )  \ start with this word to make svg start tag
    \ headern and headerv need to be setup before calling this word
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
: svgattrout ( -- caddr u )  \ this will take attributes from lineattrn and lineattrv string arrays
    \ note the lineaatrn and lineattrv string arrays are paired so ensure proper matching in pairs
    \ attribute$ can be used as an output of the last svgattrout command issued and is a string
    attribute$ init$
    lineattrv 2drop
    lineattrn swap drop 0 do
	lineattrn-$@ attribute$ $+!
	lineattrv-$@ attribute$ $+!
	s"  " attribute$ $+!
    loop attribute$ $@ ;

: svgmakepath ( -- ) \ will make path tag with lineattrn and lineattrv attributes
    \ pathdata$ needs to be populated at call time for the path data
    s" <path " svgoutput$ $+!
    svgattrout svgoutput$ $+!
    s\" d=\" " svgoutput$ $+!
    pathdata$ swap drop 0 do
	pathdata$-$@ svgoutput$ $+!
	s"  " svgoutput$ $+!
    loop
    s\" \"> </path>\n" svgoutput$ $+! ;

: svgmakecircle ( nx ny nr -- ) \ will make circle tage with lineattrn and lineattrv attributes
    \ nx is cx in circle svg
    \ ny is cy in circle svg
    \ nr is r in circle svg
    s\" <circle cx=\"" svgoutput$ $+!
    rot #to$ svgoutput$ $+!
    s\" \" cy=\"" svgoutput$ $+! swap #to$ svgoutput$ $+!
    s\" \" r=\"" svgoutput$ $+! #to$ svgoutput$ $+!
    s\" \" " svgoutput$ $+!
    svgattrout svgoutput$ $+! 
    s" />" svgoutput$ $+! ;

bufr$: textbuff$
: svgmaketext ( nx ny caddr u -- ) \ will start svg text tag and put lineattr attributes into it
    \ nx is x possition
    \ ny is y possition
    \ caddr u is the counted string to place in the text 
    textbuff$
    s\" <text x=\"" svgoutput$ $+! 2swap swap #to$ svgoutput$ $+! s\" \" y=\"" svgoutput$ $+!
    #to$ svgoutput$ $+! s\" \" " svgoutput$ $+!
    svgattrout svgoutput$ $+! s" >" svgoutput$ $+! 
    svgoutput$ $+! 
    s" </text>" svgoutput$ $+! ;

: svgend ( -- caddr u ) \ to finish the svg tag in the output string and deliver string
    s" </svg>" svgoutput$ $+!
    svgoutput$ $@ ;

: make-a-pathsvg ( -- caddr u )  \ put all the parts together and output the final svg string
    \ the attributes and path data need to be setup before calling
    svgmakehead
    svgmakepath
    svgend ;

\  ***** the stuff below here is to produce a chart using the above svg words ****
list$: localdata

variable mymin           \ will contain the chart data min absolute value
0.0e mymin f!
variable mymax           \ will contain the chart data max absolute value
0.0e mymax f!
variable xstep          \ how many x px absolute values to skip between data point plots
0.0e xstep f!
140 value xlablesize    \ the size taken up by the xlabel on the right side for chart
1300 value xmaxchart    \ the max x absolute px of the chart .. change this value to make chart larger in x
600 value ymaxchart     \ the max y absolute px of the chart .. change this value to make chart larger in y
140 value ylablesize    \ the y label at bottom of chart size in absolute px
70 value ytoplablesize  \ the y label at top of chart size in absolute px
variable yscale          \ this is the scaling factor of the y values to be placed on chart
0.0e yscale f!
9 constant xminstep     \ the min distance in px between x ploted points 
xmaxchart xminstep /
value xmaxpoints        \ this will be the max allowed points to be placed on the chart 
10 value xlableoffset   \ the offset to place lable from xlabelsize edge
10 value ylableoffset   \ the offset to place lable from ( ymaxchart + ytoplablesize )
10 value ylableqty      \ how many y lablelines and or text spots
20 value ymarksize      \ the size of the y lable marks
0 value ylabletxtpos    \ the offset of y lable text from svg window
4 value circleradius    \ the radius of the circle used on charts for lines
variable working$

: f>s ( d: -- n ) ( f: r -- )
    f>d d>s ;
: s>f ( d: n -- ) ( f: -- r )
    s>d d>f ;

: makecirclefrompathdata ( -- )
    pathdata$ swap drop 0 do
	pathdata$-$@ 32 $split 2swap 2drop 32 $split 2swap s>number?
	if
	    d>s -rot s>number? if d>s circleradius svgmakecircle else 2drop drop then   
	else
	    2drop 2drop 
	then
    loop ;

: findminmaxdata ( f: -- rmin rmax )  \ finds the min and max values of the localdata strings
    \ note this is returned on floating stack and stored in mymax and mymin floating variables
    localdata-$@ >float if fdup  mymax f! mymin f! then
    localdata swap drop xmaxpoints min 0 do
	localdata-$@ >float if fdup mymin f@ fmin mymin f! mymax f@ fmax mymax f! then
    loop mymin f@ mymax f@ ;

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

: svgchartmakepath ( -- ) \ will produce the pathdata$ string array stuff for the chart
    \ recalculate data and form the path data statement for the ploted line
    pathdata$-$off
    localdata 2drop
    s" M " working$ $! xlablesize #to$  working$ $+! s"  " working$ $+!
    localdata-$@ s>number?
    if d>f yscale f@ f* 
	mymax f@ yscale f@ f* fswap f- f>s ytoplablesize + #to$
    else
	2drop s" 0"
    then
    working$ $+! working$ $@ pathdata$-$!
    localdata swap drop xmaxpoints min 1 localdata-$@ 2drop  
    do
	s" L " working$ $!
	i s>f xstep f@ f* f>s xlablesize + #to$ working$ $+! s"  " working$ $+!
	localdata-$@ s>number?
	if d>f yscale f@ f*
	    mymax f@ yscale f@ f* fswap f- f>s ytoplablesize + #to$ working$ $+! working$ $@ pathdata$-$!
	else
	    2drop
	then
    loop ;

variable lableref$
variable lablemark$
: svgchartmakelables ( -- ) \ makes the lable lines for x and y on the chart
    \ make the y lable line
    pathdata$-$off
    s" M " working$ $! xlablesize xlableoffset - #to$ working$ $+! s"  " working$ $+!
    ytoplablesize #to$ working$ $+! s"  " working$ $+! working$ $@ 2dup lableref$ $! pathdata$-$!
    s" L " working$ $! xlablesize xlableoffset - #to$ working$ $+! s"  " working$ $+!
    ymaxchart ytoplablesize + ylableoffset + #to$ working$ $+! s"  " working$ $+! working$ $@ pathdata$-$!
    \ make the x lable line
    s" L " working$ $! xmaxchart xlablesize + #to$ working$ $+! s"  " working$ $+!
    ymaxchart ytoplablesize + ylableoffset + #to$ working$ $+! s"  " working$ $+! working$ $@ pathdata$-$!
    \ make y lable line marks
    lableref$ $@ pathdata$-$!
    s" l " working$ $! ymarksize -1 *  #to$ working$ $+! s"  " working$ $+! 0 #to$ working$ $+!
    working$ $@ 2dup lablemark$ $! pathdata$-$!
    ylableqty 1 do
	lableref$ $@ pathdata$-$!
	s" m " working$ $! 0 #to$ working$ $+! s"  " working$ $+! ymaxchart s>f ylableqty s>f f/ i s>f f* f>s #to$ working$ $+!
	working$ $@ pathdata$-$!
	lablemark$ $@ pathdata$-$!
    loop
    svgmakepath
    \ make y lable text
    svg-attrtext
    ylableqty 0 do
	ylabletxtpos ytoplablesize ymaxchart mymax f@ mymin f@ f- f>s >
	if
	    ymaxchart s>f mymax f@ mymin f@ f- f/ mymax f@ mymin f@ f- ylableqty s>f f/ f* f>s 
	else
	    mymax f@ mymin f@ f- ymaxchart s>f f/ mymax f@ mymin f@ f- ylableqty s>f f/ fswap f/ f>s 
	then i * + 
	mymax f@ mymin f@ f- ylableqty s>f f/ i s>f f* mymax f@ fswap f- f>s #to$ svgmaketext
    loop ;

: makesvgchart ( ndata-index ndata-addr -- caddr u )
    localdata-$off
    localdata->$!
    xmaxchart xminstep / to xmaxpoints    
    findminmaxdata 
    xmaxchart s>f localdata swap drop xmaxpoints min s>f f/ xstep f!
    ymaxchart s>f mymax f@ mymin f@ f- f/ yscale f!
    svgchartheader
    svgchartmakepath
    svgmakehead
    svg-attr#1     \ this is the line attribute 
    svgmakepath
    \ draw circle from the path data just made
    svg-attr#2     \ this is the circle attribute 
    makecirclefrompathdata
    svg-attr#3     \ this is lable attribute 
    svgchartmakelables
    svgend ;