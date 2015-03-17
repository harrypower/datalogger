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
require string.fs

svgmaker class
    \ these variables and values are calcuated or used in the following code
    cell% inst-var mymin      \ will contain the chart data min absolute value
    cell% inst-var mymax      \ will contain the chart data max absolute value
    cell% inst-var myspread   \ will contain mymax - mymin
    cell% inst-var xstep      \ how many x px absolute values to skip between data point plots
    cell% inst-var yscale     \ this is the scaling factor of the y values to be placed on chart
    inst-value xmaxpoints     \ this will be the max allowed points to be placed on the chart
    \ these will be strings to hold string data 
    inst-value localdata$     \ this is used by the chart making process for the data to be processed
    inst-value working$       \ this is used to work on strings temporarily in the chart code
    inst-value svgdata$       \ will contain the data values used in path 
    \ these values are inputs to the chart for changing its look or size
    \ set these values to adjust the look of the size and positions of the chart
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

    m: ( -- ) \ constructor to set some defaults
	this [parent] construct
	0.0e mymin f!
        0.0e mymax f!
        0.0e myspread f!
        0.0e xstep f!
        0.0e yscale f!
        2    [to-inst] xmaxpoints

	strings heap-new [to-inst] localdata
	strings heap-new [to-inst] working$
	strings heap-new [to-inst] svgdata$
	
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

    ;m overrides construct

    \ fudge test words ... will be deleted after object is done
    m: ( strings-data svgchartmaker -- ) \ test word to populate svgdata$
	[to-inst] svgdata$ ;m method putsvgdata$
    m: ( -- caddr u ) \ test word to show svg output
	svg-output @  @$ ;m method seeoutput

    \ some worker words to do some specific jobs
    m: ( -- )  \ finds the min and max values of the localdata strings
	\ note results stored in mymax and mymin float variables
	localdata $qty xmaxpoints min 0 ?do
	    localdata @$ >float if fdup mymin f@ fmin mymin f! mymax f@ fmax mymax f! else true throw then
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

    ;m maketext
    
    m: ( ?? -- caddr u nflag )  \ top level word to make the svg chart 

    ;m method makechart
    
end-class svgchartmaker