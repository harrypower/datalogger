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

  public
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
