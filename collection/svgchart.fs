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

svgmaker class
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
    inst-value working$       \ this is used to work on strings temporarily in the chart code
    inst-value svg-output$    \ this will be the primary output string that makechart creates
    \ these will be strings to hold strings data
    inst-value xdata$         \ holds the xdata for chart in strings
    inst-value xdata-attr$    \ holds the xdata attribute strings
    inst-value xdata-circle-attr$ \ holds the xdata circle attribute strings
    inst-value xlabdata$      \ x label data strings
    inst-value xlab-attr$     \ x label attribute strings
    inst-value ylab-attr$     \ y label attribute strings
    inst-value labline-attr$  \ label line attribute strings

    struct                    \ structure to hold the text string location and attributes
	cell% field text$
	cell% field text-x
	cell% field text-y
	cell% field text-attr$
    end-struct text%
    inst-value index-text \ text index
    inst-value addr-text  \ address of text structure
    
    m: ( -- ) \ constructor to set some defaults
	\ *** remember to add control method for testing if construct is first time running or not! ***
	this [parent] construct
	0.0e mymin f!
        0.0e mymax f!
        0.0e myspread f!
        0.0e xstep f!
        0.0e yscale f!
        2    [to-inst] xmaxpoints
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
        0    [to-inst] xlablerot
        0    [to-inst] ylablerot
	\ *** remember these items below are objects that will need to be deconstructed to prevent memory leaks ****
	string heap-new [to-inst] working$
	string heap-new [to-inst] svg-output$

	strings heap-new [to-inst] xdata$
	strings heap-new [to-inst] xdata-attr$
	strings heap-new [to-inst] xdata-circle-attr$
	strings heap-new [to-inst] xlabdata$
	strings heap-new [to-inst] xlab-attr$
	strings heap-new [to-inst] ylab-attr$
	strings heap-new [to-inst] labline-attr$
	\ *** remember the text structure is created dynamicaly so free memory if text were stored ***
	0 [to-inst] index-text 
	0 [to-inst] addr-text 
    ;m overrides construct

    \ fudge test words ... will be deleted after object is done
    m: ( -- caddr u ) \ test word to show svg output
	svg-output$ @$ ;m method seeoutput

    \ some worker methods to do some specific jobs

    m: ( nindex-text -- nstring-text nx ny nstrings-attr )
	\ to retrieve the text attributes for a given index value
	text% %size * addr-text + dup
	text$ @ swap dup
	text-x @ swap dup
	text-y @ swap 
	text-attr$ @ ;m method ntext@

    m: ( -- )  \ finds the min and max values of the localdata strings
	\ note results stored in mymax and mymin float variables
	xdata$ $qty xmaxpoints min 0 ?do
	    xdata$ @$ >float if fdup mymin f@ fmin mymin f! mymax f@ fmax mymax f! else true throw then
	loop ;m method findminmaxdata

    m: ( ?? -- ?? ) \ will produce the svg header for this chart

    ;m method makeheader
    
    m: ( ?? -- ?? ) \ will produce the cicle svg strings to be used in chart

    ;m method makecircle
    
    m: ( ?? -- ?? ) \ will produce the path strings to be used in chart

    ;m method makepath
    
    m: ( ?? -- ?? ) \ will make the chart lables both lines and text
	
    ;m method makelables

    m: ( ?? -- ?? ) \ will put the text onto the chart

    ;m method maketext

    \ methods for giving data to svgchart and geting the svg from this object
    m: ( nstrings-xdata nstrings-xdata-attr nstrings-xdata-circle-attr -- )
	\ to place xdata onto svg chart with xdata-attr and with circle-attr for each data point
	\ note the xdata is a strings object that must have quantity must match xlabdata quantity
	\ the data passed to this method is stored only once so last time it is called that data is used to make chart 
    ;m method setdata
    
    m: ( nstrings-xlabdata nstrings-xlab-attr nstrings-ylab-attr nstrings-labline-attr -- )
	\ to place xlabel data onto svg chart with x,y text and line attributes
	\ note xlabdata is a strings object containing all data to be placed on xlabel but quantity must match xdata quantity
	\ the data passed to this method is stored only once so last time it is called that data is used to make chart 
    ;m method setlabeldataattr
    
    m: ( nstring-txt nx ny nstrings-attr -- ) \ to place txt on svg with x and y location and attr
	\ every time this is called before makechart method the string,x,y and attributes are stored to be placed into svgchart
	index-text 0 >
	if
	    addr-text text% %size index-text 1 + * resize throw [to-inst] addr-text index-text 1 + [to-inst] index-text
	else
	    text% %alloc [to-inst] addr-text
	    1 [to-inst] index-text 
	then
	addr-text index-text 1 - text% %size * + dup
	-rot text-attr$ ! dup
	-rot text-y ! dup
	-rot text-x !
	text$ !
    ;m method settext
    
    m: ( ?? -- caddr u nflag )  \ top level word to make the svg chart 

    ;m method makechart
    
end-class svgchartmaker

svgchartmaker heap-new constant test
