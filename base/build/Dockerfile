FROM lambci/lambda-base

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

# A couple of packages are either missing critical-ish files, or didn't make it into the tar
# Reinstalling filesystem might not succeed fully, but continue anyway
RUN chmod 1777 /tmp && \
  /usr/bin/python3 -c "from configparser import SafeConfigParser; \
yum_conf = SafeConfigParser(); \
yum_conf.read('/etc/yum.conf'); \
yum_conf.has_section('main') or yum_conf.add_section('main'); \
yum_conf.set('main', 'plugins', '1'); \
f = open('/etc/yum.conf', 'w'); \
yum_conf.write(f); \
f.close();" && \
  rpm --rebuilddb && \
  yum install -y yum-plugin-ovl && \
  yum reinstall -y filesystem; \
  yum reinstall -y shadow-utils && \
  yum groupinstall -y development && \
  yum install -y clang cmake aws-cli docker \
    openssl-devel libffi-devel python27-devel python34-devel \
    libmpc-devel mpfr-devel gmp-devel && \
  rm -rf /var/cache/yum /var/lib/rpm/__db.* && \
  > /var/log/yum.log

RUN curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python &&  \
    pip install -U pip setuptools --no-cache-dir && \
    pip install aws-lambda-builders==0.0.1-dev
