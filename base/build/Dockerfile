FROM lambci/lambda-base

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

# A couple of packages are either missing critical-ish files, or didn't make it into the tar
# Reinstalling filesystem might not succeed fully, but continue anyway
RUN chmod 1777 /tmp && yum reinstall -y filesystem; \
  yum reinstall -y shadow-utils && \
  yum groupinstall -y development && \
  yum install -y clang cmake aws-cli docker \
    openssl-devel libffi-devel python27-devel python34-devel \
    libmpc-devel mpfr-devel gmp-devel && \
  rm -rf /var/cache/yum /var/lib/rpm/__db.* && \
  > /var/log/yum.log

