FROM lambci/lambda-base

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/bin

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
  yum install -y --releasever=latest yum-plugin-ovl && \
  yum clean all && \
  yum reinstall -y setup pam shadow-utils audit-libs iptables && \
  yum clean all && \
  yum reinstall -y --releasever=latest glibc glibc-common && \
  yum clean all && \
  yum list installed | grep installed | awk '{print $1}' | xargs yum reinstall -y && \
  yum clean all && \
  yum install -y --releasever=latest glibc-devel libdrm-devel && \
  yum clean all && \
  yum groupinstall -y development && \
  yum install -y clang cmake docker libffi-devel python27-devel python34-devel \
    libmpc-devel mpfr-devel gmp-devel cairo-devel expat-devel libicu-devel lua-devel \
    ncurses-devel readline-devel db4-devel gdbm-devel sqlite-devel keyutils-libs-devel libcom_err-devel && \
  yum clean all && \
  yum install -y --releasever=latest pcre-devel ImageMagick-devel libxml2-devel libxslt-devel && \
  yum install -y --releasever=latest libcurl-devel-$(yum list installed | grep libcurl | awk '{print $2}') && \
  yum install -y --releasever=latest krb5-devel-$(yum list installed | grep krb5 | awk '{print $2}') && \
  yum clean all && \
  yum install -y libssh2-devel && \
  rm -rf /var/cache/yum /var/lib/rpm/__db.* && \
  > /var/log/yum.log
