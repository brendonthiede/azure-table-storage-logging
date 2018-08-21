# Logging Research

This is a toy app for using Azure Table Storage to store logs. Currently only Serilog is being used.  The idea is to find a cheap way of storing logs that allows for:

* Easy Service Desk application development (for tier 1 and tier 2 support to look for certain issues)
* Easy to view a sequence of events around a known issue at a specific point in time

## Setup

Run the `scripts\autodeploy.ps1` script to deploy infrastructure and code to an Azure subscription.  Defaults attempt to use a Visual Studio Enterprise subscription.

## Recommendations

After playing with moderate volumes of log data, it seems as though you can pull entries from Azure Table Storage to your local system at around 20k records / minute. For log analysis, this seems like a reasonable amount of time if that is an entire partition and further processing of results is needed.

### Partition Key

Having a partition key of the UTC date/time rounded down to the hour seems like a good strategy for allowing smaller partitions, which in turn allows for faster searching within a time frame, without requiring too much complicated logic to pull back logs from a timespan that crosses multiple partitions.

### Row Key

Row key seems less important, but I found it useful to have the column start with the UTC time of the log entry and an atomic sequence in order to make the column easy to sort by.  I also added a GUID into the value to ensure uniqueness.