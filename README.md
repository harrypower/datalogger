This is a project that started out as a clone from my gforth_webserver repository.
Please see and or clone the gforth_webserver repository as this one will not contain any help for general consumption.

* This code needs Gforth, openbsd-inetd, sqlite3, my BBB_Gforth_gpio shared library. Get the following:
* Install stuff

```
sudo apt-get install gforth libtool libltdl-dev libffi-dev autoconf m4
sudo apt-get install openbsd-inetd
sudo apt-get install sqlite3 libsqlite3-dev
sudo apt-get install git-core emacs  # ( this is the git to pull repo and i use emacs for gforth highlight editing built into Gforth)
```

* Update gforth to version 0.7.3 or higher from the apt-get version of 0.7.0 with these steps.

```
cd too-a-temporary-location # ( maybe ~/ as example) ( a place not in the repository )
sudo mkdir gforthupdate
cd gforthupdate
sudo wget http://git.savannah.gnu.org/cgit/gforth.git/snapshot/gforth-0-7-3.tar.gz
sudo tar -xzvf gforth-0-7-3.tar.gz
cd gforth-0-7-3
sudo ./BUILD-FROM-SCRATCH --host=arm --build=arm
sudo make
sudo make install
```

Note you will get some messages and even some errors but they can all be ignored as Gforth is writen to install on may platforms!
* Now at the command line you should be able to do the following and get version 0.7.3 reported:

`gforth`

Once in the gforth environment just type bye to exit.  Version 0.7.3 should be displayed.

* Clone this repository. ( as root or user )
* Change to the repository directory
* Configure the code as follows:

`sudo ./configure`

This simply places the name of the directory the repository is at into a location that this code can find!
The file is /var/lib/datalogger-gforth/datalogger_home_path and it simply contains the full path to root of this repository on your system.
* Install the BBB_Gforth_gpio library submodule

```
sudo git submodule init
sudo git submodule update
```

* Now make shared librarys that are used in this repository. ( i may automate this in the future but here it is for now)

```
cd BBB_Gforth_gpio
gforth BBB_I2C_lib.fs ( then exit gforth with bye )
gforth BBB_GPIO_lib.fs ( then exit gforth with bye )
cd ..
cd Gforth-Tools
gforth sqlite3_gforth_lib.fs ( then exit gforth with bye )
```

The point of all that was to get gforth to recompile and link those shared librarys on your system for future use.
Note for each of those gforth environment entrys you should simply get no messages if compiling is all ok.
Now these shared librarys are stored in one of two places at this moment but needs to be copied to another place now.
If you are doing this as root then the location is /root/.gforth
If you are doing this as a user then the location is /home/yourusername/.gforth
* So the copy command if you are root is as follows:

`cp -R /root/.gforth /`  # ( this will make a directory called /.gforth and copy to all sub directorys and contents to it.)

* The command if you are a user is as follows:

`sudo cp -R /home/yourusername/.gforth /`

When you run the code that uses each of these shared librarys that code can be found by gforth and used but for some reason
when using the webserver when the librarys are called gforth can't seem to find them unless they are at / reguardless of how you set up openbsd-inetd. ( user or root )

Just for your information the shared librarys are written in forth but they literaly create c code that gets compiled with gcc
then get put into a shared library format.  When the code is accessed later gforth will link to these librarys dynamicaly to use them
rather then recompiling them and relinking them.

Note if you want to change any of the code in these librarys delete the old librarys then redo the steps above to remake the shared librarys.

In each submodule there are some basic batch scripts that will delete all the files for each shared libary.  The files are called
clean-myBBBGPIO-lib clean-myBBBi2c-lib clean_mysqlite3_gforth_lib
Note these batch files simply removed certain files from the /root/.gforth location only so you can use them but need to remove the files from the user location and the copied files from /.gforth location
* If you want to remove all the files at once simply do the following:

```
sudo rm -rf /root/.gforth
sudo rm -rf /.gforth
sudo rm -rf /home/yourusername/.gforth
```

Note ensure you type these delete commands correctly or you will delete your Linux system !

* Configure inetd service and start the webserver as follows:

`sudo nano /etc/inetd.conf`

Move the curser to the end of the document and enter the following text:

`gforth stream tcp nowait.100 yourusername /home/yourusername/datalogger/httpd.fs`

Note the path in the above line is the path for where you installed this repository.
Save the file with control x y enter
* Configure services to include gforth at some port

`sudo nano /etc/services`

Go to the end of the document and add the following line:

`gforth          4446/tcp    # my gforth webserver at port 4446`

Note you can set this port number to what ever you want but note BBB already uses port 80 and port 8080.
* Now restart the openbsd-inetd service as follows:

```
sudo /etc/init.d/openbsd-inetd restart
sudo /etc/init.d/openbsd-inetd status
```

Now you should see that the service is started and running also look at processes with ps ( `ps aux` ) should show a process called /usr/sbin/inetd in the list.
* You can stop the service at any time as follows:

`sudo /etc/init.d/openbsd-inetd stop`

A this moment the BBB will serve a page on the port you selected.

* The ADC reading code in get-co2nh3.fs needs the following added to /boot/uEnv.txt file:

`cape_enable=capemgr.enable_partno=BB-ADC`

* This will allow ADC values to be read with the following at command line (after BBB is rebooted):

`cat /sys/bus/iio/devices/iio:device0/in_voltage0_raw`

* The data collection is done with getstoresensors.fs in /collection directory.  This is a script/Gforth file that can run at boot time if done as follows:

`crontab -e`

This above command will open the file for editing of the crontab. Place the following into file before exit at bottom of the file:

`@reboot /home/pks/datalogger/collection/getstoresensors.fs &`

Note this will run the getstoresenors.fs gforth program boot up is done and collection will be every 5 min.
Note this script starts with #! but it will start gforth for the compiling of the rest of sensor-client.fs.
Data collection will be in a file called datalogged.data and this is a sqlite3 database file!
The database is stored in the collection directory and is in a file called datalogged.data.  This dbfile can be backup up with the program in that directory called usbcopydb.fs .

* This program could be set up to run in a cron job as root with the following:

`sudo crontab -e`

* Then enter this line at the bottom of the text that is in the editor at bottom of file:

`0 23 * * * /home/pi/git/datalogger/collection/usbcopydb.fs >/dev/null 2>&1`

The directory called /mnt is where the backed up copy of the datalogged.data file goes at hour 23 utc time.
The backup will be called datalogged.yearmonthday (year month and day replaced with the numbers for those values).
You can change the cron job to anything you want or even change the usbcopydb.fs program to work any way you want!
