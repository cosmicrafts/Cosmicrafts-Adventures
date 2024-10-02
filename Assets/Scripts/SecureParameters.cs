public static class SecureParameters
{
    // The Common Name (CN) you used when creating the server certificate for local testing
    public static readonly string ServerCommonName = "74.208.246.177";

    // Contents of the client CA certificate (myGameClientCA.pem)
    public static readonly string MyGameClientCA =
    @"-----BEGIN CERTIFICATE-----
MIID6TCCAtGgAwIBAgIUQUOeKGnYglJTuHWid35oajFh2GYwDQYJKoZIhvcNAQEL
BQAwgYMxCzAJBgNVBAYTAk1YMRAwDgYDVQQIDAdKYWxpc2NvMSEwHwYDVQQKDBhJ
bnRlcm5ldCBXaWRnaXRzIFB0eSBMdGQxFzAVBgNVBAMMDjc0LjIwOC4yNDYuMTc3
MSYwJAYJKoZIhvcNAQkBFhdjb250YWN0QGNvc21pY3JhZnRzLmNvbTAeFw0yNDEw
MDEyMjAwMzdaFw0yNzEwMDEyMjAwMzdaMIGDMQswCQYDVQQGEwJNWDEQMA4GA1UE
CAwHSmFsaXNjbzEhMB8GA1UECgwYSW50ZXJuZXQgV2lkZ2l0cyBQdHkgTHRkMRcw
FQYDVQQDDA43NC4yMDguMjQ2LjE3NzEmMCQGCSqGSIb3DQEJARYXY29udGFjdEBj
b3NtaWNyYWZ0cy5jb20wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDq
FXswB4DK0EC2KFVdabHTKSZ0KoVZZwBjJSJnRZaBc4mGOyF9tpx5/p7j8EKZ8xbb
MkQ5/dVswsfZOgmltWOidOgBYZzEQsFKIvOaAAs3BsWKlMwiNrQSY0aMs8E3abNG
KkWvo+JCEh9rIZk0MvJ7HRr77w33VlLzv9Pagayrk7qMpMEWKa1hlPXa1nii/3jS
myyHR/FKMhRvGPpXatvJ28lBbgEg+bXKWTrlI02T6xfjaws2IIcvxdKnePRxplVg
2Sb4zMYBSBDDov30VMcWcbQ3foUMu4I0geCpmfG6Vj2X+PRGIYQEx25qyPFqLTGB
YOCGG/EX8rkwCiCXTJXfAgMBAAGjUzBRMB0GA1UdDgQWBBQTl0UtwJl2V6zOsStl
1/oAtb94zjAfBgNVHSMEGDAWgBQTl0UtwJl2V6zOsStl1/oAtb94zjAPBgNVHRMB
Af8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQARoX1smaK527mj1G099EkygheV
1CFBtELwVrQtxo3nLfuXoNUfmCK9lDa18pNa0kq1FYRV5JRE4LEphng0JREN6cD3
0+m7UE2iOdmKCOLq2efA0oMcEC4ImsN35cgr0tGO1WzrokS4KEbaPO5a002vzQXC
lhpINOEsOMa1qj/4FB6L/v5UU41KO28zxlhAhFyfUcCZOSnTmqc0KB2a6LGYDG6A
wORPwIFYPzZI0g7M7i5DDI9MJpj72lPuyE/9wxztdE/YLmiLuy0YdEHmMTasPRR2
z9zejeCqrvoHg4n8W0nnayNnNwJbCamzIYpnbR72wM3CkM9pVXqEhc2OgERF
-----END CERTIFICATE-----";

    // Contents of the server certificate (myGameServerCertificate.pem)
    public static readonly string MyGameServerCertificate =
    @"-----BEGIN CERTIFICATE-----
MIID2DCCAsCgAwIBAgIUaDmlcHyB0NI1a0RzdjLmiZVHHXAwDQYJKoZIhvcNAQEL
BQAwgYMxCzAJBgNVBAYTAk1YMRAwDgYDVQQIDAdKYWxpc2NvMSEwHwYDVQQKDBhJ
bnRlcm5ldCBXaWRnaXRzIFB0eSBMdGQxFzAVBgNVBAMMDjc0LjIwOC4yNDYuMTc3
MSYwJAYJKoZIhvcNAQkBFhdjb250YWN0QGNvc21pY3JhZnRzLmNvbTAeFw0yNDEw
MDEyMjAxNTVaFw0yNTEwMDEyMjAxNTVaMIGDMQswCQYDVQQGEwJNWDEQMA4GA1UE
CAwHSmFsaXNjbzEhMB8GA1UECgwYSW50ZXJuZXQgV2lkZ2l0cyBQdHkgTHRkMRcw
FQYDVQQDDA43NC4yMDguMjQ2LjE3NzEmMCQGCSqGSIb3DQEJARYXY29udGFjdEBj
b3NtaWNyYWZ0cy5jb20wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC3
h8XIFZJJ/FbBr6XNRONysmuekM8qypCsc2loupiN5k8gG4NcCuE9vqr6qdzHwffO
3vnZEbdSMppFdceZeha9zlzf5bUTRERsxl6woWso5cACaLjvgDq90GOIo5pLYn3e
dprGOjsScQnMLd7Tb9RsFatIjSKKBITTSBfm5WgapwsrFUy2pvA9QkroKU/TJ1U0
ENQ/LB2fmIsq+9GUW9ZAXORvy6Rg43fAmbeTWBseEymZgdix2WRCQ0rVMUky6orJ
7WSId72czbx/LmQhB2TNdp7GEltM7xItuX6ml0PNsco4G8CsdBVMBlbpPY1NnWyx
+aKIu3BLCnTlauYaeBKvAgMBAAGjQjBAMB0GA1UdDgQWBBSsgyDTWZQgQdoyJcut
4RzXFS2Q+zAfBgNVHSMEGDAWgBQTl0UtwJl2V6zOsStl1/oAtb94zjANBgkqhkiG
9w0BAQsFAAOCAQEAfMcmC1dd46xULJe63aOebw1Xq8CexEgT4aAodUZfKssvg/CJ
l8BY84C/UIhe6asmg2UqoKhKGwrMPhZ7jYxU6gygD+6xKDb0DrSD9NDbi07EWRjB
LuaV4rf5ACL+SM03StEx25b0/TLbejgslJbyHjVzUAaD2vN3UwZHdxEUcgRtB9kW
VoBxMr95LVE6sCwVjI/eDB1cuqEfY56cFBX3IuyPO9k32v6eFZkQr14CPqrZV+gD
fO3R1WHT9OpKeaXV40ISbO8GpDMUFaddReHUjpjv+7RktUuuIU+m/TX7+ZqZ3x7C
6el1Op+D3aRwzeLeKsB1piDP+3zK9IisNe7Y4w==
-----END CERTIFICATE-----";

    // Contents of the server private key (myGameServerPrivateKey.pem)
    public static readonly string MyGameServerPrivateKey =
    @"-----BEGIN PRIVATE KEY-----
MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQC3h8XIFZJJ/FbB
r6XNRONysmuekM8qypCsc2loupiN5k8gG4NcCuE9vqr6qdzHwffO3vnZEbdSMppF
dceZeha9zlzf5bUTRERsxl6woWso5cACaLjvgDq90GOIo5pLYn3edprGOjsScQnM
Ld7Tb9RsFatIjSKKBITTSBfm5WgapwsrFUy2pvA9QkroKU/TJ1U0ENQ/LB2fmIsq
+9GUW9ZAXORvy6Rg43fAmbeTWBseEymZgdix2WRCQ0rVMUky6orJ7WSId72czbx/
LmQhB2TNdp7GEltM7xItuX6ml0PNsco4G8CsdBVMBlbpPY1NnWyx+aKIu3BLCnTl
auYaeBKvAgMBAAECggEAER4UN9evN8BV1SXNSIpqzllOyVDHSb3v5W20QKTasq1u
5tc6Fr0bCfe9Gbj7ExSHyN8qHXFWEFAQ7HaSQcHN2jEjn2xcyam92glcbov3oy5e
jLr4uLnUgLytc+KScwKCK9wTDncC6HLrj4Qdusm83cHdolwrgwTt4IjiVdJCte2C
0MO3mcV/R0vD3p9XZ1NfJZ2rVfgUXe3WggZ6r4Kpme1ffArcnx9YrsXCWT7O0LEp
rVbZqoS7spNk+0RHYv/JuoO+4dOAZimNVrsz0FLo/bpesaX9hnJg2Ia3U0a8HA9z
q5EfP9Ds+ZGZ61Px8tDI5MiJLxEiEPOFMlnzFIlQCQKBgQD1maEKz8Vfmy7jCV6Z
Af4CjPl2NWzkx8AhQYkEX7QgSXORHsAWuqnBGZ+u49yYlrz4ReLkDgzS/bPjTgFN
roYuU8dJaZ1vO4kZMpFP+/oinbZiroCPit+3WRqWjABTnL41Jl4EKTBd7aMbHNsy
kKL0InaPBPXhSZPypeuVbJ0SWQKBgQC/TUpxyzGDs07kLdAtthgxzBnnNvt/CTcG
w3EN/yMWtO2p86fz1y+mMBlLFpZ/+VFcYXDNVvNx/YviS+9v1MFOsNjBXZ3sugRO
2sHenoT2mxPqwfTL2C1p0/1rEToCGqWm/maRZgDHEw+OGSiRRx1JW3+PP745SrsV
V0JGoNBcRwKBgQCNohUPCRbHtdRqNaMKFe4IRoguNU0g+ljAVOzRSueznug4jkU0
Vl1C8KX304wJqxQ7EQJxhfC7VHeC5B84TycuXD4XBgM2fFzp3RGT0LpFcDIX4o5d
OTceoxIEM6SUk1XVjNS3DZHI+RwQrKl0FZkDtUQt4ixxyV66lhivyT0jsQKBgQCc
IAo3oKuCXp5uHu2dwNUyHu6tAvRyEyUzgeIMmEMczwCACXjyypX6vZqG66JcQy+h
g2y6SNJaH6FASNTaofo5rJ7aAPtYLeBCMsqyUxEU6i9xEmYkzwMRMY/LB74d5X14
MnunAmZ0EhxJzkKLfkxqiCAs2sr2mwTgP9y5I5mpFQKBgQDwo8aX2A6JqozCBcRD
y6lDNI+Msxa3gLpb7Ma3CQeu70dk7WG7XoXFfu4P+IDXalqcg3grDvD2SnG8PH4L
YWkUM+VowE3jz85hEbq0JZzpLNPIiBC1nYA7LI4Rv/ghY7JPirEwCwX0qk4fdUD5
xL+qqXGJRXN8odshvUjXt8gL8w==
-----END PRIVATE KEY-----";
}
