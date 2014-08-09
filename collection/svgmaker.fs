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

list$: svgattrn  \ svg attributes names to work with a line
list$: svgattrv  \ svg attribute values to work with a line but are paired with linattrn names

: svg-attr#1 ( -- ) \ initalizes svgattrn to default values
    svgattrn-$off
    s" fill="           svgattrn-$!
    s" fill-opacity="   svgattrn-$!
    s" stroke="         svgattrn-$!
    s" stroke-opacity=" svgattrn-$!
    s" stroke-width="   svgattrn-$!
    svgattrv-$off
    s\" \"rgb(255,0,0)\""   svgattrv-$!
    s\" \"0.0\""            svgattrv-$!
    s\" \"rgb(120,255,0)\"" svgattrv-$!
    s\" \"1.0\""            svgattrv-$!
    s\" \"2.0\""            svgattrv-$!
;
svg-attr#1

: svg-attr#2 ( -- ) \ second set of attributes
    svgattrn-$off
    s" fill="           svgattrn-$!
    s" fill-opacity="   svgattrn-$!
    s" stroke="         svgattrn-$!
    s" stroke-opacity=" svgattrn-$!
    s" stroke-width="   svgattrn-$!
    svgattrv-$off
    s\" \"rgb(255,0,0)\""   svgattrv-$!
    s\" \"1.0\""            svgattrv-$!
    s\" \"rgb(120,255,0)\"" svgattrv-$!
    s\" \"1.0\""            svgattrv-$!
    s\" \"3.0\""            svgattrv-$!
;

: svg-attr#3 ( -- ) \ second set of attributes
    svgattrn-$off
    s" fill="           svgattrn-$!
    s" fill-opacity="   svgattrn-$!
    s" stroke="         svgattrn-$!
    s" stroke-opacity=" svgattrn-$!
    s" stroke-width="   svgattrn-$!
    svgattrv-$off
    s\" \"rgb(0,0,255)\""   svgattrv-$!
    s\" \"0.0\""            svgattrv-$!
    s\" \"rgb(0,100,200)\"" svgattrv-$!
    s\" \"1.0\""            svgattrv-$!
    s\" \"5.0\""            svgattrv-$!
;

: svg-attrtext ( -- ) \ basic text attributes
    svgattrn-$off
    s" fill="           svgattrn-$!
    s" fill-opacity="   svgattrn-$!
    s" stroke="         svgattrn-$!
    s" stroke-opacity=" svgattrn-$!
    s" stroke-width="   svgattrn-$!
    s" font-size="      svgattrn-$!
    svgattrv-$off
    s\" \"rgb(0,0,255)\""   svgattrv-$!
    s\" \"1.0\""            svgattrv-$!
    s\" \"rgb(0,100,200)\"" svgattrv-$!
    s\" \"0.0\""            svgattrv-$!
    s\" \"4.0\""            svgattrv-$!
    s\" \"20px\""           svgattrv-$!
;

list$: svgheadern    \ header for svg .. normaly width and height 
list$: svgheaderv    \ header values that are paired with svgheadern names

: init-svg-header ( -- ) \ default svg header
    svgheadern-$off
    s" width="       svgheadern-$!
    s" height="      svgheadern-$!
    s" viewBox="     svgheadern-$!
    svgheaderv-$off
    s\" \"100\""         svgheaderv-$!
    s\" \"100\""         svgheaderv-$!
    s\" \"0 0 100 100 \"" svgheaderv-$!
;
init-svg-header

list$: svgdata$  \ the data values used in path... M,m,l,L and other path values 
: init-test-data ( -- )
    svgdata$-$off
    s" M 10 30" svgdata$-$!
    s" L 15 35" svgdata$-$!
    s" L 27 40" svgdata$-$!
    s" L 48 50" svgdata$-$!
    s" L 97 20" svgdata$-$! ;

\ The above words show example data to send the below words data and can be used to
\ populate basic structures to pass the words below!

variable svgoutput$  \ the primary output of the assembled svg string 

list$: hname
list$: hvalue
: svgmakehead ( xt-hname xt-hvalue -- )  \ start with this word to make svg start tag
    \ xt-hname is the xt for a list$: of names used in header paired with values in xt-hvalue
    \ xt-hvalue is the xt for a list$: of values used in header paired with names in xt-hname
    hname-$off
    hvalue-$off
    execute hvalue->$!
    execute hname->$!
    svgoutput$ $off  \ start svgoutput empty
    s" <svg " svgoutput$ $!
    hvalue 2drop
    hname swap drop 0 do
	hname-$@ svgoutput$ $+!
	hvalue-$@ svgoutput$ $+!
	s"  " svgoutput$ $+!
    loop
    s\" >\n" svgoutput$ $+! ;

list$: attrname$
list$: attrvalue$
: svgattrout ( xt-atrname xt-atrvalue -- )  \ takes two xt of list$: type that contain name and value
    \ xt-atrname is an xt of list$: type containing attribute name strings to be paired with xt-atrvalue
    \ xt-atrvalue is an xt of list$: type containing attribute value strings to be paired with xt-atrname
    attrname$-$off
    attrvalue$-$off
    execute attrvalue$->$!
    execute attrname$->$!
    attrvalue$ 2drop
    attrname$ swap drop 0 do
	attrname$-$@ svgoutput$ $+! 
	attrvalue$-$@ svgoutput$ $+! 
	s"  " svgoutput$ $+! 
    loop ; 

: svgmakepath ( xt-atrname xt-atrvalue xt-pathdata -- ) \ will make path tag
    \ xt-atrname is an xt of list$: type containing attribute name strings to be paired with xt-atrvalue
    \ xt-atrvalue is an xt of list$: type containing attribute value strings to be paired with xt-atrname
    \ xt-pathdata is an xt of list$: type containing the path data normaly in the d= part of path tag data
    s" <path " svgoutput$ $+!
    -rot svgattrout 
    s\" d=\" " svgoutput$ $+!
    execute swap drop 0 do
	svgdata$-$@ svgoutput$ $+!
	s"  " svgoutput$ $+!
    loop
    s\" \"> </path>\n" svgoutput$ $+! ;

: svgmakecircle ( xt-atrname xt-atrvalue nx ny nr -- ) \ will make circle tage with svgattrn and svgattrv attributes
    \ xt-atrname is an xt of list$: type containing attribute name strings to be paired with xt-atrvalue
    \ xt-atrvalue is an xt of list$: type containing attribute value strings to be paired with xt-atrname
    \ nx is cx in circle svg
    \ ny is cy in circle svg
    \ nr is r in circle svg
    s\" <circle cx=\"" svgoutput$ $+!
    rot #to$ svgoutput$ $+!
    s\" \" cy=\"" svgoutput$ $+! swap #to$ svgoutput$ $+!
    s\" \" r=\"" svgoutput$ $+! #to$ svgoutput$ $+!
    s\" \" " svgoutput$ $+!
    svgattrout 
    s" />" svgoutput$ $+! ;

bufr$: textbuff$
: svgmaketext ( xt-atrname xt-atrvalue nx ny caddr u -- ) \ will start svg text tag and put lineattr attributes into it
    \ xt-atrname is an xt of list$: type containing attribute name strings to be paired with xt-atrvalue
    \ xt-atrvalue is an xt of list$: type containing attribute value strings to be paired with xt-atrname
    \ nx is x possition
    \ ny is y possition
    \ caddr u is the counted string to place in the text 
    textbuff$
    s\" <text x=\"" svgoutput$ $+! 2swap swap #to$ svgoutput$ $+! s\" \" y=\"" svgoutput$ $+!
    #to$ svgoutput$ $+! s\" \" " svgoutput$ $+!
    2swap svgattrout s" >" svgoutput$ $+! 
    svgoutput$ $+! 
    s" </text>" svgoutput$ $+! ;

: svgend ( -- caddr u ) \ to finish the svg tag in the output string and deliver string
    s" </svg>" svgoutput$ $+!
    svgoutput$ $@ ;

: make-a-pathsvg ( xt-atrname xt-atrvalue xt-hname xt-hvalue xt-pathdata -- caddr u )
    \ put all the parts together and output the final svg string
    -rot svgmakehead
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

: makecirclefrompathdata ( xt-atrname xt-atrvalue -- )
    \ makes circle data from the existing svgdata that was used to create chart lines
    \ svgdata$ need to be populated at call time
    svgdata$ swap drop 0 do
	2dup
	svgdata$-$@ 32 $split 2swap 2drop 32 $split >float 
	if
	    >float if f>s f>s circleradius svgmakecircle else fdrop fdrop then   
	else
	    fdrop 2drop 
	then
    loop 2drop ;

: findminmaxdata ( -- )  \ finds the min and max values of the localdata strings
    \ note stored in mymax and mymin floating variables
    localdata-$@ >float if fdup  mymax f! mymin f! then
    localdata swap drop xmaxpoints min 0 do
	localdata-$@ >float if fdup mymin f@ fmin mymin f! mymax f@ fmax mymax f! then
    loop ;

: svgchartheader ( -- ) \ will produce the svgheaderv string array stuff for the chart 
    \ make header size for svg includes x and y lables 
    init-svg-header
    svgheaderv-$off
    s\" \"" working$ $!
    xmaxchart xlablesize + #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ svgheaderv-$!
    s\" \"" working$ $!
    ymaxchart ylablesize + ytoplablesize + #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ svgheaderv-$!
    s\" \"0 0 " working$ $!
    xmaxchart xlablesize + #to$ working$ $+!
    s"  " working$ $+!
    ymaxchart ylablesize + ytoplablesize + #to$ working$ $+!
    s\" \"" working$ $+!
    working$ $@ svgheaderv-$! ;

: svgchartmakepath ( -- ) \ will produce the svgdata$ string array stuff for the chart
    \ recalculate data and form the path data statement for the ploted line
    svgdata$-$off
    localdata 2drop
    s" M " working$ $! xlablesize #to$  working$ $+! s"  " working$ $+!
    localdata-$@ >float 
    if
	yscale f@ f* mymax f@ yscale f@ f* fswap f- f>s ytoplablesize + #to$
    else
	fdrop s" 0"
    then
    working$ $+! working$ $@ svgdata$-$!
    localdata swap drop xmaxpoints min 1 localdata-$@ 2drop  
    do
	s" L " working$ $!
	i s>f xstep f@ f* f>s xlablesize + #to$ working$ $+! s"  " working$ $+!
	localdata-$@ >float 
	if
	    yscale f@ f* mymax f@ yscale f@ f* fswap f- f>s ytoplablesize + #to$ working$ $+! working$ $@ svgdata$-$!
	else
	    fdrop
	then
    loop ;

variable lableref$
variable lablemark$
: svgchartmakelables (  xt-textatrname xt-textatrvalue xt-xylineatrname xt-xylinepathatrvalue -- )
    \ makes the lable lines for x and y on the chart
    \ make the y lable line
    svgdata$-$off
    s" M " working$ $! xlablesize xlableoffset - #to$ working$ $+! s"  " working$ $+!
    ytoplablesize #to$ working$ $+! s"  " working$ $+! working$ $@ 2dup lableref$ $! svgdata$-$!
    s" L " working$ $! xlablesize xlableoffset - #to$ working$ $+! s"  " working$ $+!
    ymaxchart ytoplablesize + ylableoffset + #to$ working$ $+! s"  " working$ $+! working$ $@ svgdata$-$!
    \ make the x lable line
    s" L " working$ $! xmaxchart xlablesize + #to$ working$ $+! s"  " working$ $+!
    ymaxchart ytoplablesize + ylableoffset + #to$ working$ $+! s"  " working$ $+! working$ $@ svgdata$-$!
    \ make y lable line marks
    lableref$ $@ svgdata$-$!
    s" l " working$ $! ymarksize -1 *  #to$ working$ $+! s"  " working$ $+! 0 #to$ working$ $+!
    working$ $@ 2dup lablemark$ $! svgdata$-$!
    ylableqty 1 do
	lableref$ $@ svgdata$-$!
	s" m " working$ $! 0 #to$ working$ $+! s"  " working$ $+! ymaxchart s>f ylableqty s>f f/ i s>f f* f>s #to$ working$ $+!
	working$ $@ svgdata$-$!
	lablemark$ $@ svgdata$-$!
    loop
    ['] svgdata$ svgmakepath
    ylableqty 0 do
	2dup 
	ylabletxtpos ytoplablesize
	ymaxchart s>f mymax f@ mymin f@ f- f/ mymax f@ mymin f@ f- ylableqty s>f f/ f* i s>f f* f>s + 
	mymax f@ mymin f@ f- ylableqty s>f f/ i s>f f* mymax f@ fswap f- fto$ svgmaketext
    loop 2drop ;

list$: tempattrn$
list$: tempattrv$
: makesvgchart ( ndata-index ndata-addr -- caddr u )
    localdata-$off
    localdata->$!
    xmaxchart xminstep / to xmaxpoints    
    findminmaxdata 
    xmaxchart s>f localdata swap drop xmaxpoints min s>f f/ xstep f!
    ymaxchart s>f mymax f@ mymin f@ f- f/ yscale f!
    svgchartheader  
    ['] svgheadern ['] svgheaderv svgmakehead         \ this is the header base data
    svgchartmakepath
    svg-attr#1 ['] svgattrn ['] svgattrv ['] svgdata$ svgmakepath  \ this is the line attribute 
    \ draw circle from the path data just made
    svg-attr#2 ['] svgattrn ['] svgattrv makecirclefrompathdata  \ this is the circle attribute 
    svg-attrtext svgattrn tempattrn$->$! svgattrv tempattrv$->$!
    ['] tempattrn$ ['] tempattrv$   \ lable text attribute
    svg-attr#3  ['] svgattrn ['] svgattrv   \ this is lable line attribute 
    svgchartmakelables
    svgend ;