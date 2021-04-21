# Stock, Currency and CryptoCurrency Ticker for the Elgato Stream Deck

**Author's website and contact information:** [https://barraider.com](https://barraider.com)

<img src="/_images/ticker.jpg">
<img src="/_images/currency.jpg">
<img src="/_images/crypto.jpg">

## New in v1.8
- Changed to a new Currency converter provider, as previous one is no longer available (now requires an API key).
- Increased number of currencies supported from 30 to 160+.
- Performance and stability improvements

## Features
* View live CryptoCurrencies, directly on the stream deck
* View live currencies, directly on the stream deck
* View live quotes of your favorite stocks, directly on the stream deck
* Press the Stream Deck key on quotes to see additional information (Prev. Close, Today's High/Low prices)
* Quotes have a customizable refresh rate from every 1 second, up to every 1 hour.

### Download

* [Download plugin](https://github.com/BarRaider/streamdeck-stockticker/releases/)

## I found a bug, who do I contact?
For support please contact the developer. Contact information is available at https://barraider.com

## I have a feature request, who do I contact?
Please contact the developer. Contact information is available at https://barraider.com

## Getting an API key from iexcloud.io
1. Register a free account at https://iexcloud.io/cloud-login#/register  
2. Choose the "Start" free-tier option:  
<img src="/_images/setup1.png">
3. Verify the account by clicking on the email received  
4. Go to API Tokens (in the left menu of the website)  
5. Copy the "Publishable" token and paste it into the API key field in the Stream Deck settings  
<img src="/_images/setup2.png">


## Dependencies
* Uses StreamDeck-Tools by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector) 

## Change Log

## New in v1.7
- `Stock Ticker` now also supports `Yahoo Finance`, removing the need for an API Token!
- :new: `Tarkov Ticker` shows you the live value of 0.2 BTC in RUB, matching the *Physical bitcoin* in `Escape From Tarkov`
- :new: `Stock Ticker` supports multiple stocks on the same key! Save space on your Stream Deck by having the same key rotate through multiple stocks!
- Support for customizable Background/Foreground colors + support for customizable background image
- Updated look and feel. Font size now auto-adjusts to ensure the values are shown fully on key
