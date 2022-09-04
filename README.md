# VMPoolTest_Arthrex
This Repo have 3 APIs:

1. GET: Checkout
user can checkout to a VM if available in VM Pool
returns a IP of VM

2. POST: Chechin
user can checkin from VM reserved
clean up VM details and returns Usage time

3. POST: Server Refresh
Only admin with valid password can refresh whole server
returns OK message and clears up all reservations
