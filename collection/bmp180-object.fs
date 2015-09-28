\ This Gforth code reads BMP180 pressure temperature sensor via i2c on BBB rev c hardware
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


require objects.fs
require ../BBB_Gforth_gpio/BBB_I2C_lib.fs

object class
  protected
    22      constant EEprom_size      \ the size of calibration data eeprom on bmp180 device in bytes
    0x77    constant BMP180ADDR  \ I2C address of BMP180 device
    0xF6    constant CMD_READ_VALUE
    0xAA    constant CMD_READ_CALIBRATION
    0       constant OVERSAMPLING_ULTRA_LOW_POWER \ these are the sampling constants !
    1       constant OVERSAMPLING_STANDARD
    2       constant OVERSAMPLING_HIGH_RESOLUTION
    3       constant OVERSAMPLING_ULTRA_HIGH_RESOLUTION
    1       constant i2cbus  \ note this is the linux enumerated i2c address but physically it is i2c2 not i2c1
    cell% inst-var ac1  \ signed
    cell% inst-var ac2  \ signed
    cell% inst-var ac3  \ signed
    cell% inst-var ac4  \ unsigned
    cell% inst-var ac5  \ unsigned
    cell% inst-var ac6  \ unsigned
    cell% inst-var b1   \ signed
    cell% inst-var b2   \ signed
    cell% inst-var mb   \ signed
    cell% inst-var mc   \ signed
    cell% inst-var md   \ signed
    inst-value i2c-handle
    char% 3 * inst-var buff 
    char% EEprom_size * inst-var eeprom-data
    inst-value ut
    inst-value up

    m: ( nindex bmp180 -- nsigned-cal ) \ retreave signed calibration value
	dup 1 + eeprom-data + c@ swap eeprom-data + c@ 0x100 * + 0x1000 * 0x10000 / ;m method getsigned-calvalue
    m: ( nindex bmp180 -- nunsigned-cal ) \ retrieve unsigned calibration value
	dup 1 + eeprom-data + c@ swap eeprom-data + c@ 0x100 * + ;m method getunsigned-calvalue
    m:  ( bmp180 -- )
	\ open i2c sensor channel and read eeprom
	i2cbus BMP180ADDR bbbi2copen dup 0 = throw [to-inst] i2c-handle
	i2c-handle CMD_READ_CALIBRATION bbbi2cwrite-b throw
	i2c-handle eeprom-data EEprom_size bbbi2cread 0 = throw 
	\ put eeprom data in variables
	0  this getsigned-calvalue   ac1 !
	2  this getsigned-calvalue   ac2 !
	4  this getsigned-calvalue   ac3 !
	6  this getunsigned-calvalue ac4 !
	8  this getunsigned-calvalue ac5 !
	10 this getunsigned-calvalue ac6 !
	12 this getsigned-calvalue   b1 !
	14 this getsigned-calvalue   b2 !
	16 this getsigned-calvalue   mb !
	18 this getsigned-calvalue   mc !
	20 this getsigned-calvalue   md ! 
        \ read uncompensated temperature
	0xf4 buff c!
	0x2e buff 1 + c!
	i2c-handle buff 2 bbbi2cwrite 0 = throw
	6 ms
	i2c-handle 0xf6 bbbi2cwrite-b throw
	i2c-handle buff 2 bbbi2cread 0 = throw
	buff c@ 8 lshift buff 1 + c@ or [to-inst] ut 
        \ read uncompensated pressure
	0xf4 buff c!
	0x34 OVERSAMPLING_ULTRA_LOW_POWER 6 lshift + buff 1 + c!
	i2c-handle buff 2 bbbi2cwrite 0 = throw
	OVERSAMPLING_ULTRA_LOW_POWER 1 + 10 * ms
	i2c-handle 0xf6 bbbi2cwrite-b throw
	i2c-handle buff 3 bbbi2cread 0 = throw
	buff c@ 16 lshift buff 1 + c@ 8 lshift or buff 2 + c@ or
	8 OVERSAMPLING_ULTRA_LOW_POWER - rshift [to-inst] up 
	\ close i2c channel
	i2c-handle bbbi2cclose throw ;m method doreading
    
  public
    m: ( bmp180 -- )
	this doreading
	." ac1:" ac1 @ . cr
	." ac2:" ac2 @ . cr
	." ac3:" ac3 @ . cr
	." ac4:" ac4 @ . cr
	." ac5:" ac5 @ . cr
	." ac6:" ac6 @ . cr
	." b1:" b1 @ . cr
	." b2:" b2 @ . cr
	." mb:" mb @ . cr
	." mc:" mc @ . cr
	." md:" md @ . cr
	." ut:" ut . cr
	." up:" up . cr
    ;m method printcal
    m: ( bmp180 -- ) \ default values to start talking to bmp180 sensor
	0 ac1 !
	0 ac2 !
	0 ac3 !
	0 ac4 !
	0 ac5 !
	0 ac6 !
	0 b1 !
	0 b2 !
	0 mb !
	0 mc !
	0 md !
	0 [to-inst] i2c-handle 
	buff 3 erase
	eeprom-data EEprom_size erase
    ;m overrides construct


end-class bmp180-i2c
