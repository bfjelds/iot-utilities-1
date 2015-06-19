var express = require('express');
var uwp = require('uwp');
uwp.namespace("Windows");

var gpioController = Windows.Devices.Gpio.GpioController.getDefault();
var pin = gpioController.openPin(5);	// for now this will be fixed, make it dynamic in the future.
pin.setDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.output)
var currentValue = Windows.Devices.Gpio.GpioPinValue.high;
pin.write(currentValue);

var app = express();
app.use(express.bodyParser());

app.post('/DigitalWrite', function (req, res) {
	if (!req.body.hasOwnProperty('GpioPinValue')) {
		res.statusCode = 400;
		return res.send('Error 400: Post syntax incorrect.');
	}
    
    console.log("Got a Digital Write...");	
	pin.write(req.body.GpioPinValue);	
	res.json(true);
});


app.listen(3412);