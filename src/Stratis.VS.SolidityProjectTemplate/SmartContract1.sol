// SPDX-License-Identifier: GPL-3.0
pragma solidity $soliditycompilerversion$;

contract SimpleStorage {
    uint storedData;

    function set(uint x) public {
        storedData = x;
    }

    function get() public view returns (uint) {
        return storedData;
    }
}