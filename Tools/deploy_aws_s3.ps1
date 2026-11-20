<#
.SYNOPSIS
    Automated AWS S3 Static Website Deployment Script for Unity Slot Game.
.DESCRIPTION
    Builds the standalone 'dist/' bundle and deploys it to an Amazon S3 static website bucket.
.PARAMETER BucketName
    Unique name of the S3 bucket (e.g. unity-slot-machine-priyanshu).
.PARAMETER Region
    AWS region (defaults to us-east-1).
.EXAMPLE
    .\Tools\deploy_aws_s3.ps1 -BucketName "my-unity-slot-game" -Region "us-east-1"
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$BucketName,

    [Parameter(Mandatory=$false)]
    [string]$Region = "us-east-1"
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "     AWS S3 STATIC WEBSITE DEPLOYMENT SCRIPT      " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Build dist bundle
Write-Host "`n[1/4] Building standalone distribution bundle..." -ForegroundColor Yellow
python build_web_dist.py
if (-not (Test-Path "dist/index.html")) {
    Write-Error "Build failed: dist/index.html not found."
    exit 1
}
Write-Host "Bundle ready in dist/" -ForegroundColor Green

# 2. Check AWS CLI
Write-Host "`n[2/4] Checking AWS CLI authentication..." -ForegroundColor Yellow
try {
    $identity = aws sts get-caller-identity --output json | ConvertFrom-Json
    Write-Host "Authenticated as: $($identity.Arn)" -ForegroundColor Green
} catch {
    Write-Error "AWS credentials not configured. Please run 'aws configure' first."
    exit 1
}

# 3. Create or verify S3 bucket
Write-Host "`n[3/4] Ensuring S3 bucket exists: s3://$BucketName ($Region)..." -ForegroundColor Yellow
$bucketCheck = aws s3api head-bucket --bucket $BucketName 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating new S3 bucket..."
    if ($Region -eq "us-east-1") {
        aws s3api create-bucket --bucket $BucketName --region $Region
    } else {
        aws s3api create-bucket --bucket $BucketName --region $Region --create-bucket-configuration LocationConstraint=$Region
    }
}

# Enable public read policy & website hosting
Write-Host "Configuring bucket for public static website hosting..."
aws s3api delete-public-access-block --bucket $BucketName

$policy = @"
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "PublicReadGetObject",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "s3:GetObject",
            "Resource": "arn:aws:s3:::$BucketName/*"
        }
    ]
}
"@
$policy | Out-File -FilePath "$env:TEMP\s3_policy.json" -Encoding ascii
aws s3api put-bucket-policy --bucket $BucketName --policy file://$env:TEMP\s3_policy.json
aws s3 website "s3://$BucketName" --index-document index.html

# 4. Sync files
Write-Host "`n[4/4] Syncing dist/ files to s3://$BucketName..." -ForegroundColor Yellow
aws s3 sync dist/ "s3://$BucketName" --delete

$websiteUrl = if ($Region -eq "us-east-1") {
    "http://$BucketName.s3-website-us-east-1.amazonaws.com"
} else {
    "http://$BucketName.s3-website.$Region.amazonaws.com"
}

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host "  SUCCESSFULLY DEPLOYED TO AWS S3!                " -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host "Website URL: $websiteUrl" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Green
