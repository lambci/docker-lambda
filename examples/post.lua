-- Run with:
-- wrk -s post.lua 'http://localhost:9001/2015-03-31/functions/myfunction/invocations'
wrk.method = "POST"
wrk.body   = "{}"
wrk.headers["Content-Type"] = "application/json"
