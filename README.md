# multraco - multiple transformable configs

Multraco is a Powershell cmdlet to patch MSBuild .proj files to use config transformation for many configs in one project (by default VS supports only one).
This can be useful when using together with `configSource`, i.e. extracting sections of configuration to different files. Adding multiple transformed configs allows to customize transform for each extracted configuration section.