# Command-Line Test App

This is a basic app I've used to bring up the connection to the
digital twins

## Identity

It uses 'default Azure identity', which means you need to be
logged in with `az login` using credentials which have access to
the digitial twins instance.

## URL

It looks for the URL of the instance in the `$env:TWINSURL`
environment variable.

## Actions

It simply adds some properties to some objects, just to test
the connection. This isn't meant for any long-term use.