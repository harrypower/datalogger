\ This Gforth code is BeagleBone Black Gforth SVG chart maker
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

\ The code inherits svgmaker object and creates svgchartmaker object to work with SVG
\ output for using in a web server for example!

require svgmaker2.fs
require stringobj.fs
require gforth-misc-tools.fs

svgmaker class
    cell% inst-var svgmaker-test \ used to see if construct is first being executed or not
    \ these variables and values are calcuated or used in the following code
    cell% inst-var mymin      \ will contain the chart data min absolute value
    cell% inst-var mymax      \ will contain the chart data max absolute value
    cell% inst-var myspread   \ will contain mymax - mymin
    cell% inst-var xstep      \ how many x px absolute values to skip between data point plots
    cell% inst-var yscale     \ this is the scaling factor of the y values to be placed on chart
    \ these values are inputs to the chart for changing its look or size
    \ set these values to adjust the look of the size and positions of the chart
    inst-value xmaxpoints    \ this will be the max allowed points to be placed on the chart
    inst-value xlablesize    \ the size taken up by the xlabel on the right side for chart
    inst-value xmaxchart     \ the max x absolute px of the chart .. change this value to make chart larger in x
    inst-value ymaxchart     \ the max y absolute px of the chart .. change this value to make chart larger in y
    inst-value ylablesize    \ the y label at bottom of chart size in absolute px
    inst-value ytoplablesize \ the y label at top of chart size in absolute px
    inst-value xminstep      \ the min distance in px between x ploted points
    inst-value xlableoffset  \ the offset to place lable from xlabelsize edge
    inst-value ylableoffset  \ the offset to place lable from ( ymaxchart + ytoplablesize )
    inst-value ylabletextoff \ the offset of the text from ( ymaxchart + ytoplablesize + ylabeloffset )
    inst-value ylableqty     \ how many y lablelines and or text spots
    inst-value ymarksize     \ the size of the y lable marks
    inst-value ylabletxtpos  \ the offset of y lable text from svg window
    inst-value circleradius  \ the radius of the circle used on charts for lines
    inst-value xlablerot     \ the value for rotation orientation of xlable text
    inst-value ylablerot     \ the value for rotation orientation of ylable text
    \ these will be string to hold string data 
    inst-value working$       \ used to work on a string temporarily in chart code
    \ these will be strings to hold strings data
    inst-value working$s      \ this is used to work on strings temporarily in the chart code
    inst-value pathdata$      \ contains path strings for processing in makepath and makelable
    inst-value xlabdata$      \ x label data strings
    inst-value xlab-attr$     \ x label attribute strings
    inst-value ylab-attr$     \ y label attribute strings
    inst-value labline-attr$  \ label line attribute strings

    \ structure to hold the array of datas to be charted 
    struct
	cell% field data$
	cell% field data-attr$
	cell% field circle-attr$
    end-struct data%
    inst-value index-data \ data index
    inst-value addr-data  \ addresss of data structure
    struct
    \ structure to hold the text string location and attributes
	cell% field text$
	cell% field text-x
	cell% field text-y
	cell% field text-attr$
    end-struct text%
    inst-value index-text \ text index
    inst-value addr-text  \ address of text structure
    
    m: ( -- ) \ constructor to set some defaults
	\ *** remember to add control method for testing if construct is first time running or not! ***
	svgmaker-test svgmaker-test @ =
	if
	    \ ***make word to clear only variables and reset objects and structures without memory leaks
	else
	    this [parent] construct
	    0.0e mymin sf!
	    0.0e mymax sf!
	    0.0e myspread sf!
	    0.0e xstep sf!
	    0.0e yscale sf!
	    20   [to-inst] xmaxpoints
	    140  [to-inst] xlablesize
	    1000 [to-inst] xmaxchart
	    600  [to-inst] ymaxchart
	    140  [to-inst] ylablesize
	    70   [to-inst] ytoplablesize
	    9    [to-inst] xminstep
	    10   [to-inst] xlableoffset
	    10   [to-inst] ylableoffset
	    30   [to-inst] ylabletextoff
	    10   [to-inst] ylableqty
	    20   [to-inst] ymarksize
	    0    [to-inst] ylabletxtpos
	    4    [to-inst] circleradius
	    90    [to-inst] xlablerot
	    0    [to-inst] ylablerot
	    \ *** remember these items below are objects that will need to be deconstructed to prevent memory leaks ****
	    string  heap-new [to-inst] working$
	    
	    strings heap-new [to-inst] working$s
	    strings heap-new [to-inst] pathdata$
	    strings heap-new [to-inst] xlabdata$
	    strings heap-new [to-inst] xlab-attr$
	    strings heap-new [to-inst] ylab-attr$
	    strings heap-new [to-inst] labline-attr$
	    \ *** remember the data structure is created dynamicaly so free memory if data was stored ***
	    0 [to-inst] index-data
	    0 [to-inst] addr-data
	    \ *** remember the text structure is created dynamicaly so free memory if text was stored ***
	    0 [to-inst] index-text 
	    0 [to-inst] addr-text
	    svgmaker-test svgmaker-test ! \ set flag for first time svgmaker object constructed
	then
    ;m overrides construct

    \ fudge test words ... will be deleted after object is done
    m: ( -- caddr u ) \ test word to show svg output
	svg-output @ @$ ;m method seeoutput
    m: ( --  caddr u )
	working$ @$ ;m method seeworking
    m: ( -- naddr )
	working$s ;m method working$s@
    
    m: ( f: -- fmymin fmymax fmyspread fxstep fyscale )
	mymin sf@
	mymax sf@
	myspread sf@
	xstep sf@
	yscale sf@ ;m method seecalculated
    m: ( -- naddr )
	pathdata$ ;m method seepathdata$
    m: ( -- nxlabdata$ nxlab-attr$ nylab-attr$ nlabline-attr$ )
	xlabdata$
	xlab-attr$
	ylab-attr$
	labline-attr$ ;m method seelable
    
    \ some worker methods to do some specific jobs

    m: ( nindex-data -- nstrings-xdata nstrings-xdata-attr nstrings-xdata-circle-attr )
	\ to retrieve the data and attributes for a given index value
	data% %size * addr-data + dup
	data$ @ swap dup
	data-attr$ @ swap 
	circle-attr$ @ ;m method ndata@
    
    m: ( nindex-text -- nstring-text nx ny nstrings-attr )
	\ to retrieve the text attributes for a given index value
	text% %size * addr-text + dup
	text$ @ swap dup
	text-x @ swap dup
	text-y @ swap 
	text-attr$ @ ;m method ntext@

    m: ( nxdata$ -- )  \ finds the min and max values of the localdata strings
	\ note results stored in mymax and mymin float variables
	{ xdata$ }
	xdata$ $qty xmaxpoints min 0 ?do
	    xdata$ @$x >float if fdup mymin sf@ fmin mymin sf! mymax sf@ fmax mymax sf! then
	loop ;m method findminmaxdata

    m: ( -- ) \ will produce the svg header for this chart
	working$s construct
	s\" width=" working$ !$ s\" \"" working$ !+$
	xmaxchart xlablesize + #to$ working$ !+$ s\" \"" working$ !+$
	working$ @$ working$s !$x

	s\" height=" working$ !$ s\" \"" working$ !+$
	ymaxchart ylablesize + ytoplablesize + #to$ working$ !+$ s\" \"" working$ !+$
	working$ @$ working$s !$x
	
	s\" viewBox=" working$ !$ s\" \"0 0 " working$ !+$
	xmaxchart xlablesize + #to$ working$ !+$ s"  " working$ !+$
	ymaxchart ylablesize + ytoplablesize + #to$ working$ !+$ s\" \"" working$ !+$
	working$ @$ working$s !$x

	working$s this svgheader ;m method makeheader
    
    m: ( nattr$ -- ) \ will produce the cicle svg strings to be used in chart
	\ *** use the pathdata$ to make the circle data into the svgoutput
	{ attr$ }
	attr$ reset
	pathdata$ reset
	pathdata$ $qty 0 ?do
	    s"  " pathdata$ split$s 2swap 2drop 32 $split >float
	    if
		>float if attr$ f>s f>s circleradius this svgcircle then 
	    else
		2drop
	    then
	loop
    ;m method makecircle
    
    m: ( nxdata$ -- ) \ will produce the path strings to be used in chart
	{ xdata$ }
	xdata$ reset
	s" M " working$ !$ xlablesize #to$ working$ !+$ s"  " working$ !+$
	xdata$ @$x >float
	if
	    yscale sf@ f*
	else \ if fist string is not a number just plot with mymin value
	    mymin sf@ yscale sf@ f*
	then
	mymax sf@ yscale sf@ f* fswap f- f>s ytoplablesize + #to$ working$ !+$ working$ @$ pathdata$ !$x
	xdata$ $qty xmaxpoints min 1
	?do
	    s" L " working$ !$
	    i s>f xstep sf@ f* f>s xlablesize + #to$ working$ !+$ s"  " working$ !+$
	    xdata$ @$x >float
	    if
		yscale sf@ f*
	    else \ if string is not a number just plot with mymin value
		mymin sf@ yscale sf@ f*
	    then
	    mymax sf@ yscale sf@ f* fswap f- f>s ytoplablesize + #to$ working$ !+$ working$ @$ pathdata$ !$x
	loop ;m method makepath

    
    m: ( -- ) \ will make the chart lables both lines and text
	string heap-new string heap-new string heap-new strings heap-new strings heap-new 
	{ lableref$ lablemark$ ytransform$ ytempattr$s xtempattr$s }
	pathdata$ construct
	\ make the ylable line
	s" M " working$ !$ xlablesize xlableoffset - #to$ working$ !+$ s"  " working$ !+$
	ytoplablesize #to$ working$ !+$ s"  " working$ !+$ working$ @$ 2dup lableref$ !$ pathdata$ !$x
	s" L " working$ !$ xlablesize xlableoffset - #to$ working$ !+$ s"  " working$ !+$
	ymaxchart ytoplablesize + ylableoffset + #to$ working$ !+$ s"  " working$ !+$ working$ @$ pathdata$ !$x
	\ make the xlable line
	s" L " working$ !$ xmaxchart xlablesize + #to$ working$ !+$ s"  " working$ !+$
	ymaxchart ytoplablesize + ylableoffset + #to$ working$ !+$ s"  " working$ !+$ working$ @$ pathdata$ !$x
	\ make ylable line marks
	lableref$ @$ pathdata$ !$x
	s" l " working$ !$ ymarksize -1 * #to$ working$ !+$ s"  " working$ !+$ 0 #to$ working$ !+$
	working$ @$ 2dup lablemark$ !$ pathdata$ !$x
	ylableqty 1 + 1 ?do
	    lableref$ @$ pathdata$ !$x
	    s" m " working$ !$ 0 #to$ working$ !+$ s"  " working$ !+$
	    ymaxchart s>f ylableqty s>f f/ i s>f f* f>s #to$ working$ !+$
	    working$ @$ pathdata$ !$x
	    lablemark$ @$ pathdata$ !$x
	loop
	labline-attr$ pathdata$ this svgpath
	\ generate y lable text
	ylableqty 1 + 0 ?do
	    ytempattr$s construct
	    ylab-attr$ ytempattr$s copy$s
	    ylabletxtpos ytoplablesize
	    yscale sf@ myspread sf@ ylableqty s>f f/ f* i s>f f* f>s + ( nx ny )
	    \ add transformation for ylable rotation
	    s\"  transform=\"rotate(" ytransform$ !$ ylablerot #to$ ytransform$ !+$ s" , " ytransform$ !+$
	    swap dup #to$ ytransform$ !+$ s" , " ytransform$ !+$ swap dup #to$ ytransform$ !+$
	    s"  " ytransform$ !+$ s\" )\"" ytransform$ !+$ ytransform$ @$ ytempattr$s !$x ytempattr$s -rot
	    myspread sf@ ylableqty s>f f/ i s>f f* mymax sf@ fswap f- fto$ ytransform$ !$ ytransform$ 
	    this svgtext 
	loop
	\ generate x lable text  
	xlabdata$ $qty 0 ?do
	    xtempattr$s construct
	    xlab-attr$ xtempattr$s copy$s
	    xlablesize xmaxchart s>f xlabdata$ $qty s>f f/ i s>f f* f>s + ylableoffset ymaxchart + ytoplablesize + ylabletextoff +
	    s\"  transform=\"rotate(" working$ !$ xlablerot #to$ working$ !+$ s" , " working$ !+$
	    swap dup #to$ working$ !+$ s" , " working$ !+$ swap dup #to$ working$ !+$ s"  " working$ !+$
	    s\" )\"" working$ !+$ working$ @$ xtempattr$s !$x xtempattr$s -rot xlabdata$
	    this svgtext
	loop
	
	lableref$ destruct
	lablemark$ destruct
	ytransform$ destruct
	ytempattr$s destruct
	xtempattr$s destruct
    ;m method makelables

    m: ( ?? -- ?? ) \ will put the text onto the chart

    ;m method maketext

    \ methods for giving data to svgchart and geting the svg from this object
    m: ( nstrings-xdata nstrings-xdata-attr nstrings-xdata-circle-attr -- )
	\ to place xdata onto svg chart with xdata-attr and with circle-attr for each data point
	\ note the xdata is a strings object that must have quantity must match xlabdata quantity
	\ the data passed to this method is stored only once so last time it is called that data is used to make chart 
	index-data 0 >
	if
	    addr-data data% %size index-data 1 + * resize throw [to-inst] addr-data index-data 1 + [to-inst] index-data
else
	    data% %alloc [to-inst] addr-data
	    1 [to-inst] index-data
	then
	addr-data index-data 1 - data% %size * + dup
	-rot circle-attr$ strings heap-new dup rot ! copy$s dup
	-rot data-attr$ strings heap-new dup rot ! copy$s
	data$ strings heap-new dup rot ! copy$s
    ;m method setdata
    
    m: ( nstrings-xlabdata nstrings-xlab-attr nstrings-ylab-attr nstrings-labline-attr -- )
	\ to place xlabel data onto svg chart with x,y text and line attributes
	\ note xlabdata is a strings object containing all data to be placed on xlabel but quantity must match xdata quantity
	\ the data passed to this method is stored only once so last time it is called that data is used to make chart 
	labline-attr$ copy$s
	ylab-attr$ copy$s
	xlab-attr$ copy$s
	xlabdata$ copy$s ;m method setlabledataattr
    
    m: ( nstring-txt nx ny nstrings-attr -- ) \ to place txt on svg with x and y location and attributes
	\ every time this is called before makechart method the string,x,y and attributes are stored to be placed into svgchart
	index-text 0 >
	if
	    addr-text text% %size index-text 1 + * resize throw [to-inst] addr-text index-text 1 + [to-inst] index-text
	else
	    text% %alloc [to-inst] addr-text
	    1 [to-inst] index-text 
	then
	addr-text index-text 1 - text% %size * + dup
	-rot text-attr$ strings heap-new dup rot ! copy$s dup
	-rot text-y ! dup
	-rot text-x !
	text$ string heap-new dup rot ! swap @$ rot !$
    ;m method settext
    
    m: ( -- caddr u nflag )  \ top level word to make the svg chart 
	\ test for data available for chart making and valid
	index-data 0 = if abort" No data for chart!" then
	0 this ndata@ 2drop $qty 0 = if abort" Data for chart is empty!" then
	xlabdata$ $qty 0 = if abort" No lable data for chart!" then
	0 this ndata@ 2drop $qty index-data 1 ?do dup i this ndata@ 2drop $qty <> if abort" Data quantitys not the same!" then loop
	xlabdata$ $qty <> if abort" Data quantitys not the same!" then 
	\ set mymin and mymax to start values
	0 this ndata@ 2drop dup reset @$x >float if fdup mymax sf! mymin sf! else abort" Data not a number!" then  
	\ find all min and max values from all data sets
	index-data 0 ?do
	    i this ndata@ 2drop dup reset this findminmaxdata
	loop
	\ calculate myspread
	mymax sf@ mymin sf@ f- myspread sf!
	\ calculate xstep
	xmaxchart s>f 0 this ndata@ 2drop $qty xmaxpoints min s>f f/ xstep sf!
	\ calculate yscale
	ymaxchart s>f myspread sf@ f/ yscale sf!
	\ execute makeheader
	this makeheader
	\ make path and cicle svg elements with there associated attributes 
	index-data 0 ?do
	    i this ndata@ swap rot this makepath pathdata$ this svgpath this makecircle
	loop
	\ execute makelables
	this makelables
	\ execute maketext
	\ finish svg with svgend to return the svg string
	this svgend
	\ return false if no other errors
	false
    ;m method makechart
    
end-class svgchartmaker

svgchartmaker heap-new constant test


strings heap-new constant tdata
strings heap-new constant tda
strings heap-new constant tdca
strings heap-new constant tld
strings heap-new constant tla
strings heap-new constant ta
string heap-new constant tt

s\" fill=\"rgb(0,0,255)\""     ta !$x
s\" fill-opacity=\"1.0\""      ta !$x
s\" stroke=\"rgb(0,100,200)\"" ta !$x
s\" stroke-opacity=\"0.0\""    ta !$x
s\" stroke-width=\"4.0\""      ta !$x
s\" font-size=\"20px\""        ta !$x

s" 10"                         tdata !$x
s" 20"                         tdata !$x
s" 53.9"                       tdata !$x
s" 0.789"                      tdata !$x

s\" fill=\"rgb(255,0,0)\""     tda !$x
s\" fill-opacity=\"0.0\""      tda !$x
s\" stroke=\"rgb(120,255,0)\"" tda !$x
s\" stroke-opacity=\"1.0\""    tda !$x
s\" stroke-width=\"2.0\""      tda !$x

s\" fill=\"rgb(255,0,0)\""     tdca !$x
s\" fill-opacity=\"1.0\""      tdca !$x
s\" stroke=\"rgb(120,255,0)\"" tdca !$x
s\" stroke-opacity=\"1.0\""    tdca !$x
s\" stroke-width=\"3.0\""      tdca !$x

s" first"                      tld !$x
s" second"                     tld !$x
s" third"                      tld !$x
s" fourth"                     tld !$x

\ s\" fill=\"rgb(255,0,0)\""     tla !$x
s\" fill-opacity=\"0.0\""      tla !$x
s\" stroke=\"rgb(120,255,0)\"" tla !$x
s\" stroke-opacity=\"1.0\""    tla !$x
s\" stroke-width=\"3.0\""      tla !$x

s" her is first text"          tt !$

tt 10 20  ta test settext
s" second texts" tt !$
tt 30 30 ta test settext

tld tla tla tla test setlabledataattr

tdata tda tdca test setdata
tdata construct
s" 19" tdata !$x
s" 29" tdata !$x
s" 3.92" tdata !$x
s" 99.3" tdata !$x
tdata tda tdca test setdata

cr
test seelable $qty . $qty . $qty . $qty . ." lable" cr

0 test ntext@ $qty . . . @$ type space ." text" cr
1 test ntext@ $qty . . . @$ type space ." text" cr

0 test ndata@ $qty . $qty . @$x type space ." first data set point 0" cr
1 test ndata@ $qty . $qty . @$x type space ." second data set point 0" cr