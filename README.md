# Stock, Currency and CryptoCurrency Ticker for the Elgato Stream Deck

**Author's website and contact information:** [https://barraider.com](https://barraider.com)

<img src="/_images/ticker.jpg">
<img src="/_images/currency.jpg">
<img src="/_images/crypto.jpg">

## New in v1.6
* Supports modifying the multiplier in the Currency Converter. Useful when the difference between two currencies is very bug (like Japanese Yen and Pound)

## New in v1.4
**IMPORTANT:** The provider of the data we use (iexcloud.io) now requires a Free API token. I am working on finding a free solution that does not require an API token. In the mean time, see the ***Getting an API key*** section below for details on how to get a free one from iexcloud.io.

## Getting an API key
1. Register a free account at https://iexcloud.io/cloud-login#/register  
2. Choose the "Start" free-tier option:  
<img src="/_images/setup1.png">
3. Verify the account by clicking on the email received  
4. Go to API Tokens (in the left menu of the website)  
5. Copy the "Publishable" token and paste it into the API key field in the Stream Deck settings  
<img src="/_images/setup2.png">

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

## Dependencies
* Uses StreamDeck-Tools by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector) 

