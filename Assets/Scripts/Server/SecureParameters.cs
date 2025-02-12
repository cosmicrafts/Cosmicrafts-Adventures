public static class SecureParameters
{
    // The Common Name (CN) you used when creating the server certificate for local testing
    public static readonly string ServerCommonName = "play.cosmicrafts.com";

    // Contents of the client CA certificate (myGameClientCA.pem)
    public static readonly string MyGameClientCA =
    @"-----BEGIN CERTIFICATE-----
MIIEVzCCAj+gAwIBAgIRAIOPbGPOsTmMYgZigxXJ/d4wDQYJKoZIhvcNAQELBQAw
TzELMAkGA1UEBhMCVVMxKTAnBgNVBAoTIEludGVybmV0IFNlY3VyaXR5IFJlc2Vh
cmNoIEdyb3VwMRUwEwYDVQQDEwxJU1JHIFJvb3QgWDEwHhcNMjQwMzEzMDAwMDAw
WhcNMjcwMzEyMjM1OTU5WjAyMQswCQYDVQQGEwJVUzEWMBQGA1UEChMNTGV0J3Mg
RW5jcnlwdDELMAkGA1UEAxMCRTUwdjAQBgcqhkjOPQIBBgUrgQQAIgNiAAQNCzqK
a2GOtu/cX1jnxkJFVKtj9mZhSAouWXW0gQI3ULc/FnncmOyhKJdyIBwsz9V8UiBO
VHhbhBRrwJCuhezAUUE8Wod/Bk3U/mDR+mwt4X2VEIiiCFQPmRpM5uoKrNijgfgw
gfUwDgYDVR0PAQH/BAQDAgGGMB0GA1UdJQQWMBQGCCsGAQUFBwMCBggrBgEFBQcD
ATASBgNVHRMBAf8ECDAGAQH/AgEAMB0GA1UdDgQWBBSfK1/PPCFPnQS37SssxMZw
i9LXDTAfBgNVHSMEGDAWgBR5tFnme7bl5AFzgAiIyBpY9umbbjAyBggrBgEFBQcB
AQQmMCQwIgYIKwYBBQUHMAKGFmh0dHA6Ly94MS5pLmxlbmNyLm9yZy8wEwYDVR0g
BAwwCjAIBgZngQwBAgEwJwYDVR0fBCAwHjAcoBqgGIYWaHR0cDovL3gxLmMubGVu
Y3Iub3JnLzANBgkqhkiG9w0BAQsFAAOCAgEAH3KdNEVCQdqk0LKyuNImTKdRJY1C
2uw2SJajuhqkyGPY8C+zzsufZ+mgnhnq1A2KVQOSykOEnUbx1cy637rBAihx97r+
bcwbZM6sTDIaEriR/PLk6LKs9Be0uoVxgOKDcpG9svD33J+G9Lcfv1K9luDmSTgG
6XNFIN5vfI5gs/lMPyojEMdIzK9blcl2/1vKxO8WGCcjvsQ1nJ/Pwt8LQZBfOFyV
XP8ubAp/au3dc4EKWG9MO5zcx1qT9+NXRGdVWxGvmBFRAajciMfXME1ZuGmk3/GO
koAM7ZkjZmleyokP1LGzmfJcUd9s7eeu1/9/eg5XlXd/55GtYjAM+C4DG5i7eaNq
cm2F+yxYIPt6cbbtYVNJCGfHWqHEQ4FYStUyFnv8sjyqU8ypgZaNJ9aVcWSICLOI
E1/Qv/7oKsnZCWJ926wU6RqG1OYPGOi1zuABhLw61cuPVDT28nQS/e6z95cJXq0e
K1BcaJ6fJZsmbjRgD5p3mvEf5vdQM7MCEvU0tHbsx2I5mHHJoABHb8KVBgWp/lcX
GWiWaeOyB7RP+OfDtvi2OsapxXiV7vNVs7fMlrRjY1joKaqmmycnBvAq14AEbtyL
sVfOS66B8apkeFX2NY4XPEYV4ZSCe8VHPrdrERk2wILG3T/EGmSIkCYVUMSnjmJd
VQD9F6Na/+zmXCc=
-----END CERTIFICATE-----";

    // Contents of the server certificate (myGameServerCertificate.pem)
    public static readonly string MyGameServerCertificate =
    @"-----BEGIN CERTIFICATE-----
MIIDhzCCAw6gAwIBAgISBCcBhiHP0abCZ3PlSsYf6Z4pMAoGCCqGSM49BAMDMDIx
CzAJBgNVBAYTAlVTMRYwFAYDVQQKEw1MZXQncyBFbmNyeXB0MQswCQYDVQQDEwJF
NTAeFw0yNDEwMjAwMzAzMDdaFw0yNTAxMTgwMzAzMDZaMB8xHTAbBgNVBAMTFHBs
YXkuY29zbWljcmFmdHMuY29tMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEssC1
tsrkSLqgo5hfj2uC3PR8uAsIfu88QUnYWKCjfnlAfRvmbofs8kFotfpzWEyW6R4T
PH+vSUqRsV4xyZLqdaOCAhUwggIRMA4GA1UdDwEB/wQEAwIHgDAdBgNVHSUEFjAU
BggrBgEFBQcDAQYIKwYBBQUHAwIwDAYDVR0TAQH/BAIwADAdBgNVHQ4EFgQUQMxC
jCneK0Fm2JPspTj7TmCFkL4wHwYDVR0jBBgwFoAUnytfzzwhT50Et+0rLMTGcIvS
1w0wVQYIKwYBBQUHAQEESTBHMCEGCCsGAQUFBzABhhVodHRwOi8vZTUuby5sZW5j
ci5vcmcwIgYIKwYBBQUHMAKGFmh0dHA6Ly9lNS5pLmxlbmNyLm9yZy8wHwYDVR0R
BBgwFoIUcGxheS5jb3NtaWNyYWZ0cy5jb20wEwYDVR0gBAwwCjAIBgZngQwBAgEw
ggEDBgorBgEEAdZ5AgQCBIH0BIHxAO8AdgDm0jFjQHeMwRBBBtdxuc7B0kD2loSG
+7qHMh39HjeOUAAAAZKoFnnmAAAEAwBHMEUCIB9BSCXjpkuj/G5Sr85EKJnOsW/i
v2LEQokmllDGQ8jgAiEA2n0/g3pwlffqQCZrUq/ym4ilYDDzUep3gKdOiA829U0A
dQATSt8atZhCCXgMb+9MepGkFrcjSc5YV2rfrtqnwqvgIgAAAZKoFnrSAAAEAwBG
MEQCICDuPlC0Dj/8gQTbeMEONW3Y46Ie9XhtiTyuO4su3vUnAiArPk2CM4ylujdJ
b/yRqQlE/gdyJURvp+sNSNtsDC+lYDAKBggqhkjOPQQDAwNnADBkAjBYvkfnrI26
LHg1H/z7g8c70K0NMRZ/Eu62lBIf2Gk1Reu+XJR0vus2guHdi8uH+F8CMHz6P2xo
0xyc9MBF0yebe36WWwwUHcy87WDAMamNm71dMGXtxvnDC1plPB9JKlii8A==
-----END CERTIFICATE-----";

    // Contents of the server private key (myGameServerPrivateKey.pem)
    public static readonly string MyGameServerPrivateKey =
    @"-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQg3LCZxf6aTtu18Clb
FXXIEnwq8GN3ILypEwNzLAx1EIyhRANCAASywLW2yuRIuqCjmF+Pa4Lc9Hy4Cwh+
7zxBSdhYoKN+eUB9G+Zuh+zyQWi1+nNYTJbpHhM8f69JSpGxXjHJkup1
-----END PRIVATE KEY-----";
}
