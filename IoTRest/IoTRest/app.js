var express = require('express');
var app = express();

var bodyParser = require('body-parser');

app.use(bodyParser.json());


var uwp = require("uwp");
uwp.projectNamespace("Windows");

var gpioController = Windows.Devices.Gpio.GpioController.getDefault();
var pin = gpioController.openPin(5);	// for now this will be fixed, make it dynamic in the future.
pin.setDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.output)
var currentValue = Windows.Devices.Gpio.GpioPinValue.high;
pin.write(currentValue);

app.get('/', function (req, res) {
    res.send('hello world');
});

app.post('/DigitalWrite', function (req, res) {
    
    if (!req.body.hasOwnProperty('GpioPinValue')) {
        res.statusCode = 400;
        return res.send('Error 400: DigitalWrite/GpioPinValue - Post syntax incorrect.');
    }
    
    console.log("Got a Digital Write...");
    pin.write(req.body.GpioPinValue);
    res.json(true);
});

module.exports = app

console.log('we are up and running...');

app.listen(3142);

