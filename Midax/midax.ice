// **********************************************************************
//
// Copyright (c) 2003-2012 ZeroC, Inc. All rights reserved.
//
// This copy of Ice is licensed to you under the terms described in the
// ICE_LICENSE file included in this distribution.
//
// **********************************************************************

#pragma once

module Midax
{

interface MidaxIce
{
	idempotent string ping();
	idempotent void startsignals();
	idempotent void stopsignals();
	idempotent void shutdown();
	idempotent string getStatus();
	idempotent void log(string message, long logType);
	idempotent void tick(string mktDataId, long year, long month, long day, 
			long hours, long minutes, long seconds, long milliseconds,
			double price, long volume);
};

};
