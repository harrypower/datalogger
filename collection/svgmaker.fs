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

list$: svgattr  \ svg attributes names to work with a line

: svg-attr#1 ( -- ) \ initalizes svgattr to default values
    svgattr-$off
    s\" fill=\"rgb(255,0,0)\""      svgattr-$!
    s\" fill-opacity=\"0.0\""       svgattr-$!
    s\" stroke=\"rgb(120,255,0)\""  svgattr-$!
    s\" stroke-opacity=\"1.0\""     svgattr-$!
    s\" stroke-width=\"2.0\""       svgattr-$! ;
svg-attr#1

: svg-attr#2 ( -- ) \ second set of attributes
    svgattr-$off
    s\" fill=\"rgb(255,0,0)\""      svgattr-$!
    s\" fill-opacity=\"1.0\""       svgattr-$!
    s\" stroke=\"rgb(120,255,0)\""  svgattr-$!
    s\" stroke-opacity=\"1.0\""     svgattr-$!
    s\" stroke-width=\"3.0\""       svgattr-$! ;

: svg-attr#3 ( -- ) \ second set of attributes
    svgattr-$off
    s\" fill=\"rgb(0,0,255)\""      svgattr-$!
    s\" fill-opacity=\"0.0\""       svgattr-$!
    s\" stroke=\"rgb(0,100,200)\""  svgattr-$!
    s\" stroke-opacity=\"1.0\""     svgattr-$!
    s\" stroke-width=\"5.0\""       svgattr-$! ;

: svg-attrtext ( -- ) \ basic text attributes
    svgattr-$off
    s\" fill=\"rgb(0,0,255)\""      svgattr-$!
    s\" fill-opacity=\"1.0\""       svgattr-$!
    s\" stroke=\"rgb(0,100,200)\""  svgattr-$!
    s\" stroke-opacity=\"0.0\""     svgattr-$!
    s\" stroke-width=\"4.0\""       svgattr-$!
    s\" font-size=\"20px\""         svgattr-$! ;

list$: svgheader    \ header for svg .. normaly width and height 

: init-svg-header ( -- ) \ default svg header
    svgheader-$off
    s\" width=\"100\""            svgheader-$!
    s\" height=\"100\""           svgheader-$!
    s\" viewBox=\"0 0 100 100 \"" svgheader-$! ;
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

variable tempxt$
: xt>list$:-$@ ( xt-list$: -- xt-list$:-$@ )  \ from an xt of list$: type the iterator name is found and xt of that is returned
    >name dup 0 = throw name>string tempxt$ $! s" -$@" tempxt$ $+! tempxt$ $@ find-name dup 0 = throw name>int ;
: xt>list$:-$! ( xt-list$: -- xt-list$:-$! ) \ from an xt of list$: type the storage name is found and xt of that is returned
    >name dup 0 = throw name>string tempxt$ $! s" -$!" tempxt$ $+! tempxt$ $@ find-name dup 0 = throw name>int ;

: svgmakehead ( xt-header -- )  \ start with this word to make svg start tag
    \ xt-header is the xt for a list$: of names used in header 
    svgoutput$ $off  \ start svgoutput empty
    s" <svg " svgoutput$ $!
    dup xt>list$:-$@ swap
    execute swap drop 0 ?do
	dup execute svgoutput$ $+!
	s"  " svgoutput$ $+!
    loop drop 
    s\" >\n" svgoutput$ $+! ;

: svgattrout ( xt-atrname -- )  \ takes two xt of list$: type that contain name and value
    \ xt-atrname is an xt of list$: type containing attribute name and value strings
    dup xt>list$:-$@ swap 
    execute swap drop 0 ?do
	dup execute svgoutput$ $+! 
	s"  " svgoutput$ $+! 
    loop drop ; 

: svgmakepath ( xt-atrname xt-pathdata -- ) \ will make path tag
    \ xt-atrname is an xt of list$: type containing attribute name and value strings 
    \ xt-pathdata is an xt of list$: type containing the path data normaly in the d= part of path tag data
    s" <path " svgoutput$ $+!
    swap svgattrout 
    s\" d=\" " svgoutput$ $+!
    dup xt>list$:-$@ swap 
    execute swap drop 0 ?do
	dup execute svgoutput$ $+!
	s"  " svgoutput$ $+!
    loop drop 
    s\" \"> </path>\n" svgoutput$ $+! ;

: svgmakecircle ( xt-atrname nx ny nr -- ) \ will make circle tag with svgattr attributes
    \ xt-atrname is an xt of list$: type containing attribute name value strings
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
: svgmaketext ( xt-atrname nx ny caddr u -- ) \ will start svg text tag and put attributes into it
    \ xt-atrname is an xt of list$: type containing attribute name value strings
    \ nx is x possition
    \ ny is y possition
    \ caddr u is the counted string to place in the text
    textbuff$
    s\" <text x=\"" svgoutput$ $+! 2swap swap #to$ svgoutput$ $+! s\" \" y=\"" svgoutput$ $+!
    #to$ svgoutput$ $+! s\" \" " svgoutput$ $+!
    rot svgattrout s" >" svgoutput$ $+! 
    svgoutput$ $+! 
    s\" </text>" svgoutput$ $+! ;

: svgend ( -- caddr u ) \ to finish the svg tag in the output string and deliver string
    s" </svg>" svgoutput$ $+!
    svgoutput$ $@ ;

: make-a-pathsvg ( xt-atrname xt-header xt-pathdata -- caddr u )
    \ put all the parts together and output the final svg string
    swap svgmakehead
    svgmakepath
    svgend ;

\ ********************************************************************************
\  ***** the stuff below here is to produce a chart using the above svg words ****

\ these variables and values are calcuated or used in the following code
variable mymin          \ will contain the chart data min absolute value
0.0e mymin f!
variable mymax          \ will contain the chart data max absolute value
0.0e mymax f!
variable myspread       \ will contain mymax - mymin 
0.0e myspread f!
variable xstep          \ how many x px absolute values to skip between data point plots
0.0e xstep f!
variable yscale         \ this is the scaling factor of the y values to be placed on chart
0.0e yscale f!
2 value xmaxpoints      \ this will be the max allowed points to be placed on the chart 
list$: localdata        \ this is used by the chart making process for the data to be processed
variable working$       \ this is used to work on strings temporarily in the chart code

\ these values are inputs to the chart for changing its look or size
\ set these values to adjust the look of the size and positions of the chart 
140 value xlablesize    \ the size taken up by the xlabel on the right side for chart
1000 value xmaxchart    \ the max x absolute px of the chart .. change this value to make chart larger in x
600 value ymaxchart     \ the max y absolute px of the chart .. change this value to make chart larger in y
140 value ylablesize    \ the y label at bottom of chart size in absolute px
70 value ytoplablesize  \ the y label at top of chart size in absolute px
9 value xminstep        \ the min distance in px between x ploted points 
10 value xlableoffset   \ the offset to place lable from xlabelsize edge
10 value ylableoffset   \ the offset to place lable from ( ymaxchart + ytoplablesize )
30 value ylabletextoff  \ the offset of the text from ( ymaxchart + ytoplablesize + ylabeloffset )
10 value ylableqty      \ how many y lablelines and or text spots
20 value ymarksize      \ the size of the y lable marks
0 value ylabletxtpos    \ the offset of y lable text from svg window
4 value circleradius    \ the radius of the circle used on charts for lines
0 value xlablerot       \ the value for rotation orientation of xlable text
0 value ylablerot       \ the value for rotation orientation of ylable text

: f>s ( d: -- n ) ( f: r -- )
    f>d d>s ;
: s>f ( d: n -- ) ( f: -- r )
    s>d d>f ;

: makecirclefrompathdata ( xt-atrname  -- )
    \ makes circle data from the existing svgdata that was used to create chart lines
    \ svgdata$ need to be populated at call time
    svgdata$ swap drop 0 ?do
	dup
	svgdata$-$@ 32 $split 2swap 2drop 32 $split >float 
	if
	    >float if f>s f>s circleradius svgmakecircle then   
	else
	    2drop 
	then
    loop drop ;

: findminmaxdata ( -- )  \ finds the min and max values of the localdata strings
    \ note results stored in mymax and mymin floating variables
    localdata swap drop xmaxpoints min 0 ?do
	localdata-$@ >float if fdup mymin f@ fmin mymin f! mymax f@ fmax mymax f! else true throw then
    loop ;

: svgchartheader ( -- ) \ will produce the svgheaderv string array stuff for the chart 
    \ make header size for svg including x and y lables
    \ Note this uses the default header from init-svg-header and adds the correct size values
    svgheader-$off
    s\" width=" working$ $! s\" \"" working$ $+!
    xmaxchart xlablesize + #to$ working$ $+! s\" \"" working$ $+!
    working$ $@ svgheader-$!

    s\" height=" working$ $! s\" \"" working$ $+!
    ymaxchart ylablesize + ytoplablesize + #to$ working$ $+! s\" \"" working$ $+!
    working$ $@ svgheader-$!
    
    s\" viewBox=" working$ $! s\" \"0 0 " working$ $+!
    xmaxchart xlablesize + #to$ working$ $+! s"  " working$ $+!
    ymaxchart ylablesize + ytoplablesize + #to$ working$ $+! s\" \"" working$ $+!
    working$ $@ svgheader-$! ;

: svgchartmakepath ( -- ) \ will produce the svgdata$ string array stuff for the chart
    \ recalculate data and form the path data statement for the ploted line
    svgdata$-$off
    localdata 2drop
    s" M " working$ $! xlablesize #to$  working$ $+! s"  " working$ $+!
    localdata-$@ >float 
    if
	yscale f@ f* mymax f@ yscale f@ f* fswap f- f>s ytoplablesize + #to$ working$ $+! working$ $@ svgdata$-$!
    else \ first string not a number so just plot it with mymin value 
	 mymin f@ yscale f@ f* mymax f@ yscale f@ f* fswap f- f>s ytoplablesize + #to$ working$ $+! working$ $@ svgdata$-$! 
    then
    \ working$ $+! working$ $@ svgdata$-$!
    localdata swap drop xmaxpoints min 1 localdata-$@ 2drop  
    ?do
	s" L " working$ $!
	i s>f xstep f@ f* f>s xlablesize + #to$ working$ $+! s"  " working$ $+!
	localdata-$@ >float 
	if
	    yscale f@ f* mymax f@ yscale f@ f* fswap f- f>s ytoplablesize + #to$ working$ $+! working$ $@ svgdata$-$!
	else \ if string is not a number plot it with mymin value 
	    mymin f@ yscale f@ f* mymax f@ yscale f@ f* fswap f- f>s ytoplablesize + #to$ working$ $+! working$ $@ svgdata$-$!
	then
    loop ;

variable lableref$
variable lablemark$
list$: ytempattr$
variable ytransform$
: svgchartmakelables (  ylabtxt-attr-xt%  labline-attr-xt%  -- )
    \ makes the lable lines for x and y on the chart
    \ make the y lable text 
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
    ylableqty 1 + 1 ?do
	lableref$ $@ svgdata$-$!
	s" m " working$ $! 0 #to$ working$ $+! s"  " working$ $+! ymaxchart s>f ylableqty s>f f/ i s>f f* f>s #to$ working$ $+!
	working$ $@ svgdata$-$!
	lablemark$ $@ svgdata$-$!
    loop
    ['] svgdata$ svgmakepath
    \ generate y lable text
    ylableqty 1 + 0 ?do
	dup  ( ylabtxt-attr-xt% )
	ytempattr$-$off
	execute ytempattr$->$!  \ copied  ylabtxt-attr-xt% to ytempattr$ now ready to add the transformation to it
	['] ytempattr$
	ylabletxtpos ytoplablesize  
	yscale f@ myspread f@ ylableqty s>f f/ f* i s>f f* f>s + ( nx-text ny-text )
	\ add transformation for ylable rotation
	s\"  transform=\"rotate(" ytransform$ $! ylablerot #to$ ytransform$ $+! s" , " ytransform$ $+!
	swap dup #to$ ytransform$ $+! s" , " ytransform$ $+! swap dup #to$ ytransform$ $+! s"  "
	ytransform$ $+! s\" )\"" ytransform$ $+! ytransform$ $@ ytempattr$-$! 
	myspread f@ ylableqty s>f f/ i s>f f* mymax f@ fswap f- fto$ svgmaketext 
    loop
    drop  ;

variable templable$
list$: xtempattr$
: svgchartXlabletext ( xlabtxt-attr-xt% xlable-data-xt% -- )
    \ make x lable text from xlable-data-xt% list$: string array
    0 0 0 { xaxt xdxt xqty x$xt xa$xt }
    xdxt xt>list$:-$@ to x$xt
    xdxt execute swap drop dup to xqty 0 ?do
	xtempattr$-$off
	xaxt execute xtempattr$->$!
	['] xtempattr$
	xlablesize xmaxchart s>f xqty s>f f/ i s>f f* f>s +  ylableoffset ymaxchart + ytoplablesize + ylabletextoff +
	s\"  transform=\"rotate(" templable$ $! xlablerot #to$ templable$ $+! s" , " templable$ $+!
	swap dup #to$ templable$ $+! s" , " templable$ $+! swap dup #to$ templable$ $+! s"  "
	templable$ $+! s\" )\"" templable$ $+! templable$ $@ xtempattr$-$!
	x$xt execute svgmaketext
    loop ;


\ this structure contains xt's that are created with list$:
\ this is the method to pass the chart data and chart attributes to makesvgchart
struct
    cell% field data-xt%
    cell% field data-attr-xt%
    cell% field circle-attr-xt%
end-struct chartdata%
struct 
    cell% field xlable-data-xt%
    cell% field xlabtxt-attr-xt%
    cell% field ylabtxt-attr-xt%
    cell% field labline-attr-xt%
end-struct chartattr%

struct
    cell% field text-xt%
    cell% field text-x%
    cell% field text-y%
    cell% field text-attr-xt%
end-struct charttext%

: svgcharttext ( charttext% textqty )
    { text% textqty }
   \ textqty 1 >= if
    textqty 0 ?do
	text% text-attr-xt% i charttext% %size * + @
	text% text-x% i charttext% %size * + @
	text% text-y% i charttext% %size * + @
	text% text-xt% i charttext% %size * + @ xt>list$:-$@ execute
	svgmaketext
    loop \ then 
;

0 value xdataqty
: makesvgchart ( ncharttext% ncharttextqty nchartattr% nchartdata% nchartdataqty -- caddr u nflag )
    \ note if each dataset has a different amount of data nflag will be true
    \ nflag will be false if svg generated for chart
    \ nflag is true if the a data string is no a number
    try
	{ text% textqty attr% data% dataqty }
	0.0e mymax f! 0.0e mymin f!
	xmaxchart xminstep / to xmaxpoints    
	localdata-$off
	data% data-xt% 0 chartdata% %size * + @ execute dup to xdataqty localdata->$!
	localdata-$@ >float if fdup  mymax f! mymin f! else true throw then \ start min max at the first value
	dataqty 0 ?do    \ find all datasets min and max values
	    localdata-$off
	    data% data-xt% i chartdata% %size * + @ execute dup xdataqty <> throw localdata->$!
	    findminmaxdata
	loop
	mymax f@ mymin f@ f- myspread f!
	xmaxchart s>f localdata swap drop xmaxpoints min s>f f/ xstep f!
	ymaxchart s>f myspread f@ f/ yscale f!
	svgchartheader  
	['] svgheader svgmakehead         \ this is the header data
	dataqty 0 ?do
	    localdata-$off
	    data% data-xt% i chartdata% %size * + @ execute localdata->$!
	    svgchartmakepath
	    data% data-attr-xt% i chartdata% %size * + @
	    ['] svgdata$ svgmakepath  \ this is the line attribute 
	    \ draw circle from the path data just made
	    data% circle-attr-xt% i chartdata% %size * + @
	    makecirclefrompathdata    \ this is the circle attribute 
	loop
	attr% ylabtxt-attr-xt% @    \ y lable text attribute
	attr% labline-attr-xt% @    \ this is lable line attribute 
	svgchartmakelables
	attr% xlabtxt-attr-xt% @    \ this is x lable text attributes
	attr% xlable-data-xt% @
	svgchartXlabletext
	text% textqty svgcharttext  \ this places text where ever the text% structure says to
	svgend
	false
    restore dup if swap drop then 
    endtry ;